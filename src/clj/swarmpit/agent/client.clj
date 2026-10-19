(ns swarmpit.agent.client
  (:require [swarmpit.http :refer :all]
            [swarmpit.ip :refer [is-valid-url]]
            [swarmpit.config :refer [config]]
            [cheshire.core :refer [generate-string]]))

(defn- execute
  [{:keys [method url api options quiet-statuses timeout]}]
  (execute-in-scope {:method          method
                     :url             (str url api)
                     :options         options
                     :scope           "Agent"
                     :timeout         timeout
                     :error-handler   #(or (:detail %) %)
                     :quiet-statuses  quiet-statuses}))

(defn info
  [agent-url]
  (-> (execute {:method :GET
                :api    "/"
                :url    agent-url})
      :body))

(defn volumes
  "The node's volume list in the engine's /volumes shape (agent 2.3.2+). The
   agent itself gives up after 10s on a wedged volume plugin."
  [agent-url]
  (-> (execute {:method         :GET
                :api            "/volumes"
                :url            agent-url
                :timeout        12000
                :quiet-statuses #{404}})
      :body))

(defn logs
  [agent-url container-id since]
  (when (not-empty container-id)
    (try
      (-> (execute {:method          :GET
                    :api             (str "/logs/" container-id)
                    :url             agent-url
                    :quiet-statuses  #{400 404}
                    :options         {:query-params
                                      (merge {}
                                             (when since
                                               {:since since}))}})
          :body)
      (catch Exception ex
        (let [e (ex-data ex)]
          (if (.contains [400 404] (:status e))
            nil
            (throw ex)))))))
