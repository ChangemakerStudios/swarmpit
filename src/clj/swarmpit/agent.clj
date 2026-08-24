(ns swarmpit.agent
  (:require [swarmpit.api :as api]
            [chime.core :as chime]
            [taoensso.timbre :refer [info debug warn error]])
  (:import (clojure.lang ExceptionInfo)
           (java.time Instant Duration)))

(defn- autoredeploy-job
  []
  (when-let [services (try
                        (->> (api/services)
                             (filter #(get-in % [:deployment :autoredeploy]))
                             (filter #(not= "updating" (get-in % [:status :update]))))
                        (catch Exception e
                          (error "Autoredeploy: failed to fetch services" (.getMessage e))
                          nil))]
    (doseq [service services]
      (let [id (:id service)
            name (:serviceName service)
            repository (:repository service)]
        (try
          (let [current-digest (:imageDigest repository)
                latest-digest (api/repository-digest nil
                                                     (:name repository)
                                                     (:tag repository))]
            (when (not= current-digest
                        latest-digest)
              (api/redeploy-service nil id nil latest-digest)
              (info "Service" id (str "(" name ")") "autoredeploy fired! DIGEST:" (str "[" current-digest "] -> [" latest-digest "]"))))
          (catch ExceptionInfo e
            (let [status (:status (ex-data e))]
              (if (= 404 status)
                (debug "Service" id (str "(" name ")") "autoredeploy check: image not found in registry")
                (error "Service" id (str "(" name ")") "autoredeploy failed!" (ex-data e))))))))))

(defonce ^:private reported-task-failures (atom #{}))

(defn- task-failure-job
  "Surface task placement failures in the log. A service update returns 200 the
   moment the daemon accepts it - the swarm only rejects the task afterwards, on
   the node, so failures like \"no basic auth credentials\" never reach our log
   on their own. Each task is reported once; the id set is reset when it grows
   past a sane bound so it cannot leak."
  []
  (try
    (let [failed (->> (api/tasks)
                      (filter #(contains? #{"rejected" "failed"} (:state %)))
                      (filter #(some? (get-in % [:status :error]))))]
      (when (< 2000 (count @reported-task-failures))
        (reset! reported-task-failures #{}))
      (doseq [{:keys [id serviceName nodeName state] :as task} failed]
        (when-not (contains? @reported-task-failures id)
          (swap! reported-task-failures conj id)
          (warn "Task" id (str "(" serviceName ")") state "on node" nodeName
                "-" (get-in task [:status :error])))))
    (catch Exception e
      (debug "Task failure check skipped:" (.getMessage e)))))

(defn init []
  (let [start (.plusSeconds (Instant/now) 60)]
    (chime/chime-at
      (chime/periodic-seq start (Duration/ofMinutes 1))
      (fn [time]
        (autoredeploy-job)
        (task-failure-job)))))