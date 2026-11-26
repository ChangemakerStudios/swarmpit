(ns swarmpit.stats-test
  (:require [clojure.test :refer :all]
            [clojure.core.cache :as cache]
            [swarmpit.stats :as stats]))

(def ^:private node-stats*
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
    (stats/store-to-cache node-stats*)
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

(defn- with-state
  [hosts cached f]
  (let [original @stats/cache]
    (try
      (reset! stats/cache (reduce (fn [c s] (assoc c (:id s) (assoc s :receivedAt (System/currentTimeMillis))))
                                  (cache/basic-cache-factory {})
                                  cached))
      (with-redefs [stats/active-hosts (constantly (set hosts))]
        (f))
      (finally (reset! stats/cache original)))))

(def ^:private node-stats
  {:id     "node1"
   :cpu    {:usedPercentage 10}
   :memory {:usedPercentage 20 :used 200 :total 1000}
   :disk   {:usedPercentage 30 :used 300 :total 1000}})

(deftest not-ready-reason-test
  (testing "nodes up but no agent has reported yet (#735)"
    (with-state ["node1"] []
      #(is (= :no-agent-data (stats/not-ready-reason)))))

  (testing "an empty cache is reported without touching docker"
    (let [original @stats/cache]
      (try
        (reset! stats/cache (cache/basic-cache-factory {}))
        (with-redefs [stats/active-hosts #(throw (ex-info "docker is down" {}))]
          (is (= :no-agent-data (stats/not-ready-reason))))
        (finally (reset! stats/cache original)))))

  (testing "agent data but no active swarm nodes"
    (with-state [] [node-stats]
      #(is (= :no-active-nodes (stats/not-ready-reason)))))

  (testing "cached stats belong to nodes that are gone"
    (with-state ["node2"] [node-stats]
      #(is (= :stale-agent-data (stats/not-ready-reason)))))

  (testing "ready once an active node has reported"
    (with-state ["node1"] [node-stats]
      #(do (is (nil? (stats/not-ready-reason)))
           (is (true? (stats/ready?)))))))

(deftest cluster-test
  (with-redefs [stats/hosts-resources (constantly {})
                stats/cluster-cpus (constantly 8)]
    (testing "averages across the active nodes that reported"
      (with-state ["node1"] [node-stats]
        #(let [{:keys [cpu memory disk]} (stats/cluster)]
           (is (= 10 (:usage cpu)))
           (is (= 20 (:usage memory)))
           (is (= 200 (:used memory)))
           (is (= 1000 (:total memory)))
           (is (= 30 (:usage disk))))))

    (testing "two nodes are averaged, not summed, for usage"
      (with-state ["node1" "node2"]
                  [node-stats (assoc node-stats :id "node2"
                                                :cpu {:usedPercentage 30})]
        #(let [{:keys [cpu memory]} (stats/cluster)]
           (is (= 20 (:usage cpu)))
           (is (= 400 (:used memory))))))

    (testing "no active node with stats yields zeroes instead of dividing by zero"
      (with-state ["node2"] [node-stats]
        #(let [{:keys [cpu memory disk]} (stats/cluster)]
           (is (= 0 (:usage cpu)))
           (is (= 0 (:usage memory)))
           (is (= 0 (:usage disk))))))))

(deftest store-to-cache-test
  (testing "a push without a node id is ignored rather than cached under nil"
    (with-state ["node1"] []
      #(do (stats/store-to-cache {:cpu {:usedPercentage 1}})
           (is (empty? @stats/cache)))))

  (testing "a push with a node id is cached under that id"
    (with-state ["node1"] []
      #(do (stats/store-to-cache node-stats)
           (is (= node-stats (dissoc (get @stats/cache "node1") :receivedAt)))
           (is (number? (get-in @stats/cache ["node1" :receivedAt])))))))
