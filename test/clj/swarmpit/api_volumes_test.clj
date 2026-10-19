(ns swarmpit.api-volumes-test
  (:require [clojure.test :refer :all]
            [swarmpit.api :as api]
            [swarmpit.agent.client :as sac]
            [swarmpit.docker.engine.client :as dc]))

(defn- vol
  ([name driver scope] (vol name driver scope nil))
  ([name driver scope cluster]
   (cond-> {:Name name :Driver driver :Scope scope :Mountpoint "" :Options nil :Labels nil}
     cluster (assoc :ClusterVolume {:ID "c1"}))))

(defn- agent-task
  [node ip]
  {:NodeID              node
   :Status              {:State "running"}
   :NetworksAttachments [{:Addresses [(str ip "/24")]}]})

(def ^:private node-volumes
  {"http://10.0.3.2:8080" {:Volumes [(vol "seq_ecn" "flasharray:latest" "global")
                                     (vol "seq_ecn" "local" "local")
                                     (vol "tmp" "local" "local")]}
   "http://10.0.3.4:8080" ::unreachable})

(defn- with-swarm
  [f]
  (with-redefs [dc/info (fn [] {:Swarm {:NodeID "n1"}})
                dc/nodes (fn [] [{:ID "n1" :Description {:Hostname "dev01"}}
                                 {:ID "n2" :Description {:Hostname "dev02"}}
                                 {:ID "n3" :Description {:Hostname "dev03"}}
                                 {:ID "n4" :Description {:Hostname "dev04"}}])
                dc/volumes (fn [& _] {:Volumes [(vol "seq_ecn" "flasharray:latest" "global")
                                                (vol "tmp" "local" "local")
                                                (vol "pending" "local" "global" true)]})
                ;; n3 runs no agent; n4's agent fails
                dc/service-tasks-by-label (fn [& _] [(agent-task "n1" "10.0.3.1")
                                                     (agent-task "n2" "10.0.3.2")
                                                     (agent-task "n4" "10.0.3.4")])
                sac/volumes (fn [url]
                              (let [v (get node-volumes url)]
                                (if (= ::unreachable v)
                                  (throw (ex-info "Agent error: Request timeout" {}))
                                  v)))]
    (f)))

(defn- row
  [rows name node]
  (first (filter #(and (= name (:volumeName %)) (= node (:node %))) rows)))

(deftest volumes-on-nodes-test
  (with-swarm
    #(let [rows (api/volumes-on-nodes)]
       (testing "a global volume is listed once, without a node"
         (is (= 1 (count (filter (fn [v] (= "flasharray:latest" (:driver v))) rows))))
         (is (nil? (:node (row rows "seq_ecn" nil)))))

       (testing "same-named local volumes on two nodes are separate rows"
         (is (= {:nodeName "dev01" :remote false} (select-keys (row rows "tmp" "n1") [:nodeName :remote])))
         (is (= {:nodeName "dev02" :remote true} (select-keys (row rows "tmp" "n2") [:nodeName :remote]))))

       (testing "a local copy named like a shared volume shadows it"
         (is (true? (:shadows (row rows "seq_ecn" "n2"))))
         (is (nil? (:shadows (row rows "tmp" "n2")))))

       (testing "a cluster volume stays a single node-less row"
         (is (true? (:cluster (row rows "pending" nil)))))

       (testing "nodes without an agent or with a failing one are skipped"
         (is (= #{"n1" "n2"} (set (keep :node rows))))))))
