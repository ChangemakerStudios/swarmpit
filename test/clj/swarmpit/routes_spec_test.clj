(ns swarmpit.routes-spec-test
  (:require [clojure.test :refer :all]
            [clojure.spec.alpha :as s]
            [spec-tools.data-spec :as ds]
            [swarmpit.routes-spec :as routes-spec]))

(def mounts-spec
  (ds/spec ::mounts (:mounts routes-spec/service-update)))

(def resources-spec
  (ds/spec ::resources routes-spec/service-resources))

(deftest service-update-mounts-spec-test
  (testing "GET output for a bind mount (volumeOptions null) round-trips through update"
    (is (s/valid? mounts-spec
                  [{:containerPath "/etc/localtime"
                    :host          "/etc/localtime"
                    :type          "bind"
                    :readOnly      true
                    :volumeOptions nil}])))

  (testing "omitted volumeOptions is valid"
    (is (s/valid? mounts-spec
                  [{:containerPath "/data"
                    :host          "my-volume"
                    :type          "volume"
                    :readOnly      false}])))

  (testing "GET output for a volume mount without options (null fields) round-trips through update"
    (is (s/valid? mounts-spec
                  [{:containerPath "/data"
                    :host          "my-volume"
                    :type          "volume"
                    :id            "my-volume"
                    :stack         nil
                    :readOnly      false
                    :volumeOptions {:labels nil
                                    :driver {:name    nil
                                             :options nil}}}])))

  (testing "populated volumeOptions is valid"
    (is (s/valid? mounts-spec
                  [{:containerPath "/data"
                    :host          "my-volume"
                    :type          "volume"
                    :readOnly      false
                    :volumeOptions {:labels {}
                                    :driver {:name    "local"
                                             :options [{:name "type" :value "nfs"}]}}}]))))

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
