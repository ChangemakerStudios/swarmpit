(ns swarmpit.stats-test
  (:require [clojure.test :refer :all]
            [clojure.core.cache :as cache]
            [swarmpit.stats :as stats]))

(def ^:private node-stats
  {:id     "n1"
   :cpu    {:usedPercentage 50}
   :memory {:usedPercentage 40 :used 4 :total 10}
   :disk   {:usedPercentage 30 :used 3 :total 10}
   :tasks  [{:name "/web.1.t1" :cpuPercentage 10}]})

(defn- with-cache [received-at f]
  (with-redefs [stats/cache (atom (cache/basic-cache-factory {}))
                stats/nodes-memo (constantly [{:ID          "n1"
                                               :Status      {:State "ready"}
                                               :Description {:Resources {:NanoCPUs    4000000000
                                                                         :MemoryBytes 10}}}])]
    (stats/store-to-cache node-stats)
    (when received-at
      (swap! stats/cache assoc-in ["n1" :receivedAt] received-at))
    (f)))

(def ^:private task {:nodeId "n1" :taskName "web.1" :id "t1" :resources {:limit {:cpu 0}}})

(deftest fresh-stats-are-served
  (with-cache nil
    #(do
       (is (false? (:stale (stats/node "n1"))))
       (is (some? (:updatedAt (stats/node "n1"))))
       (is (some? (stats/task-raw task)))
       (is (= 50 (get-in (stats/cluster) [:cpu :usage]))))))

(deftest stale-stats-are-flagged-and-excluded
  (with-cache (- (System/currentTimeMillis) stats/stale-after-ms 1000)
    #(do
       (is (true? (:stale (stats/node "n1"))))
       (is (nil? (stats/task-raw task)))
       (is (= 0 (get-in (stats/cluster) [:cpu :usage])))
       (is (= 0 (get-in (stats/cluster) [:memory :used]))))))
