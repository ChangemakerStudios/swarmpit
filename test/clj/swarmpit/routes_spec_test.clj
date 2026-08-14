(ns swarmpit.routes-spec-test
  (:require [clojure.test :refer :all]
            [clojure.spec.alpha :as s]
            [spec-tools.data-spec :as ds]
            [swarmpit.routes-spec :as rs]))

(def resources-spec (ds/spec ::resources rs/service-resources))

(deftest service-resources-spec-test
  (testing "clean-nils output shapes are accepted"
    ;; blank memory limit in the UI drops :memory (or the whole :limit) from the payload
    (is (s/valid? resources-spec {}))
    (is (s/valid? resources-spec {:limit {:cpu 0}}))
    (is (s/valid? resources-spec {:reservation {:memory 0 :cpu 0}}))
    (is (s/valid? resources-spec {:reservation {:memory 0 :cpu 0}
                                  :limit       {:cpu 0}})))

  (testing "complete resources are accepted"
    (is (s/valid? resources-spec {:reservation {:memory 0 :cpu 0}
                                  :limit       {:memory 1024 :cpu 0.5}})))

  (testing "non-numeric values are rejected"
    (is (not (s/valid? resources-spec {:limit {:memory "1gb"}})))
    (is (not (s/valid? resources-spec {:limit {:cpu "half"}})))))
