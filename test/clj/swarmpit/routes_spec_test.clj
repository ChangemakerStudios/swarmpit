(ns swarmpit.routes-spec-test
  (:require [clojure.test :refer :all]
            [clojure.spec.alpha :as s]
            [spec-tools.data-spec :as ds]
            [swarmpit.routes-spec :as routes-spec]))

(def mounts-spec
  (ds/spec ::mounts (:mounts routes-spec/service-update)))

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

  (testing "populated volumeOptions is valid"
    (is (s/valid? mounts-spec
                  [{:containerPath "/data"
                    :host          "my-volume"
                    :type          "volume"
                    :readOnly      false
                    :volumeOptions {:labels {}
                                    :driver {:name    "local"
                                             :options [{:name "type" :value "nfs"}]}}}]))))
