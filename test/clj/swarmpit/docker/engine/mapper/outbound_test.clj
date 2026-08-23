(ns swarmpit.docker.engine.mapper.outbound-test
  (:require [clojure.test :refer :all]
            [swarmpit.docker.engine.mapper.outbound :refer [->service-image ->service-mounts]]))

(deftest ->service-image-test
  (let [service {:repository {:name "nginx" :tag "alpine" :imageDigest "sha256:abc"}}]
    (testing "digest? true with digest uses tag+digest form"
      (is (= "nginx:alpine@sha256:abc" (->service-image service true))))

    (testing "digest? false uses tag form"
      (is (= "nginx:alpine" (->service-image service false)))))

  (testing "blank imageDigest falls back to tag form (issue #724)"
    (is (= "nginx:alpine"
           (->service-image {:repository {:name "nginx" :tag "alpine" :imageDigest ""}} true)))
    (is (= "nginx:alpine"
           (->service-image {:repository {:name "nginx" :tag "alpine" :imageDigest "   "}} true))))

  (testing "nil imageDigest falls back to tag form"
    (is (= "nginx:alpine"
           (->service-image {:repository {:name "nginx" :tag "alpine"}} true)))))

(deftest ->service-mounts-test
  (testing "bind mount with nil volumeOptions omits VolumeOptions entirely"
    (let [[mount] (->service-mounts {:mounts [{:containerPath "/etc/localtime"
                                               :host          "/etc/localtime"
                                               :type          "bind"
                                               :readOnly      true
                                               :volumeOptions nil}]})]
      (is (= {:ReadOnly true
              :Source   "/etc/localtime"
              :Target   "/etc/localtime"
              :Type     "bind"} mount))
      (is (not (contains? mount :VolumeOptions)))))

  (testing "bind mount with empty volumeOptions map omits VolumeOptions (docker rejects it for non-volume mounts)"
    (let [[mount] (->service-mounts {:mounts [{:containerPath "/etc/localtime"
                                               :host          "/etc/localtime"
                                               :type          "bind"
                                               :readOnly      true
                                               :volumeOptions {}}]})]
      (is (not (contains? mount :VolumeOptions)))))

  (testing "volume mount keeps VolumeOptions"
    (let [[mount] (->service-mounts {:mounts [{:containerPath "/data"
                                               :host          "my-volume"
                                               :type          "volume"
                                               :readOnly      false
                                               :volumeOptions {:labels {:foo "bar"}
                                                               :driver {:name    "local"
                                                                        :options [{:name "type" :value "nfs"}]}}}]})]
      (is (= {:Labels       {:foo "bar"}
              :DriverConfig {:Name    "local"
                             :Options {:type "nfs"}}}
             (:VolumeOptions mount))))))
