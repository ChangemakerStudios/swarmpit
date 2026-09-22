(ns swarmpit.maintenance-test
  (:require [clojure.test :refer :all]
            [swarmpit.maintenance :as maintenance]))

(deftest command-test
  (is (= ["docker" "volume" "prune" "--force"]
         (maintenance/command {:kind "volumes"})))
  (is (= ["docker" "volume" "prune" "--force" "--all"]
         (maintenance/command {:kind "volumes" :all true})))
  (is (= ["docker" "image" "prune" "--force"]
         (maintenance/command {:kind "images"})))
  (is (= ["docker" "image" "prune" "--force" "--all"]
         (maintenance/command {:kind "images" :all true})))
  (is (= ["docker" "system" "prune" "--force"]
         (maintenance/command {:kind "system"})))
  (is (= ["docker" "system" "prune" "--force" "--all" "--volumes"]
         (maintenance/command {:kind "system" :images true :volumes true}))))

(deftest reclaimed-test
  (is (= "1.2GB" (maintenance/reclaimed ["Deleted Volumes:" "abc" "" "Total reclaimed space: 1.2GB"])))
  (is (nil? (maintenance/reclaimed ["Error response from daemon: boom"]))))

(deftest prune-rejects-unknown-kind
  (is (thrown-with-msg? clojure.lang.ExceptionInfo #"Unknown prune kind"
                        (maintenance/prune! {:kind "everything"}))))
