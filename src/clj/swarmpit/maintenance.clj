(ns swarmpit.maintenance
  "Cluster-wide prune. Docker's prune API only cleans the daemon it is called on,
   so each run is a short-lived global-job service that executes the docker cli
   against every node's own socket; results are read back from its task logs."
  (:require [clojure.tools.logging :as log]
            [swarmpit.config :as cfg]
            [swarmpit.docker.engine.client :as dc]
            [swarmpit.docker.engine.log :as dl]
            [swarmpit.uuid :refer [uuid]]))

(def ^:private job-label "swarmpit.maintenance")

(def ^:private timeout-ms 600000)

(def ^:private poll-ms 2000)

(def ^:private terminal-states #{"complete" "failed" "rejected" "shutdown" "orphaned" "remove"})

(defonce ^:private current (atom nil))

(defn- now [] (str (java.time.Instant/now)))

(defn command
  [{:keys [kind all images volumes]}]
  (case kind
    "volumes" (cond-> ["docker" "volume" "prune" "--force"]
                all (conj "--all"))
    "images" (cond-> ["docker" "image" "prune" "--force"]
               all (conj "--all"))
    "system" (cond-> ["docker" "system" "prune" "--force"]
               images (conj "--all")
               volumes (conj "--volumes"))))

(defn- job-spec
  [id cmd]
  {:Name         (str "swarmpit-maintenance-" (subs id 0 8))
   :Labels       {job-label "prune"}
   :Mode         {:GlobalJob {}}
   :TaskTemplate {:ContainerSpec {:Image   (cfg/config :maintenance-image)
                                  :Command cmd
                                  :Labels  {job-label "prune"}
                                  :Mounts  [{:Type   "bind"
                                             :Source "/var/run/docker.sock"
                                             :Target "/var/run/docker.sock"}]}
                  :RestartPolicy {:Condition "none"}}})

(defn- schedulable-nodes
  "Nodes a global job will place a task on."
  []
  (->> (dc/nodes)
       (filter #(and (= "ready" (get-in % [:Status :State]))
                     (= "active" (get-in % [:Spec :Availability]))))))

(defn- remove-leftovers!
  "A swarmpit restart mid-run leaves its job service behind."
  []
  (doseq [{:keys [ID]} (dc/services job-label)]
    (try
      (dc/delete-service ID)
      (catch Exception e
        (log/warn "Failed to remove leftover maintenance service" ID (.getMessage e))))))

(defn- wait-for-tasks
  [service-id expected]
  (let [deadline (+ (System/currentTimeMillis) timeout-ms)]
    (loop []
      (let [tasks (dc/service-tasks service-id)
            done? (and (>= (count tasks) expected)
                       (every? #(terminal-states (get-in % [:Status :State])) tasks))]
        (if (or done? (> (System/currentTimeMillis) deadline))
          {:tasks tasks :timed-out? (not done?)}
          (do (Thread/sleep poll-ms)
              (recur)))))))

(defn reclaimed
  [lines]
  (some #(second (re-find #"Total reclaimed space:\s*(.+)$" %)) lines))

(defn- node-result
  [task node-names lines]
  (let [state (get-in task [:Status :State])
        node-id (:NodeID task)]
    {:node      (get node-names node-id node-id)
     :state     state
     :error     (get-in task [:Status :Err])
     :reclaimed (reclaimed lines)
     :output    (vec lines)}))

(defn- run-job!
  [job cmd]
  (let [expected (count (schedulable-nodes))
        node-names (->> (dc/nodes)
                        (map (juxt :ID #(get-in % [:Description :Hostname])))
                        (into {}))
        service-id (:ID (dc/create-service nil (job-spec (:id job) cmd)))]
    (try
      (let [{:keys [tasks timed-out?]} (wait-for-tasks service-id expected)
            lines-by-task (->> (dl/parse-service-log (dc/service-logs service-id 0))
                               (group-by :task))]
        (assoc job
          :status (if timed-out? "timeout" "done")
          :finishedAt (now)
          :nodes (->> tasks
                      (map #(node-result % node-names (map :line (get lines-by-task (:ID %)))))
                      (sort-by :node)
                      (vec))))
      (finally
        (try
          (dc/delete-service service-id)
          (catch Exception e
            (log/warn "Failed to remove maintenance service" service-id (.getMessage e))))))))

(defn status [] @current)

(defn prune!
  [{:keys [kind] :as options}]
  (when-not (#{"volumes" "images" "system"} kind)
    (throw (ex-info "Unknown prune kind"
                    {:status 400 :type :api :body {:error (str "Unknown prune kind: " kind)}})))
  (let [job {:id        (uuid)
             :kind      kind
             :options   (select-keys options [:all :images :volumes])
             :status    "running"
             :startedAt (now)}
        previous @current
        started? (and (not= "running" (:status previous))
                      (compare-and-set! current previous job))]
    (when-not started?
      (throw (ex-info "Prune already running"
                      {:status 409 :type :api :body {:error "A prune is already running"}})))
    (future
      (reset! current
              (try
                (remove-leftovers!)
                (run-job! job (command options))
                (catch Exception e
                  (log/error e "Prune failed")
                  (assoc job :status "failed"
                             :finishedAt (now)
                             :error (or (get-in (ex-data e) [:body :error]) (.getMessage e)))))))
    job))
