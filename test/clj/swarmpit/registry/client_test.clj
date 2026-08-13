(ns swarmpit.registry.client-test
  (:require [clojure.test :refer :all]
            [swarmpit.registry.client :refer :all]
            [swarmpit.http :as http])
  (:import (clojure.lang ExceptionInfo)
           (java.io IOException)))

(deftest registry-client-test
  (testing "dns error"
    (with-redefs [http/execute-in-scope (fn [_] (throw (ex-info "Registry failure: not-existing-addr: Name or service not known"
                                                               {:status 500
                                                                :type :http-client
                                                                :body {:error "not-existing-addr: Name or service not known"}})))]
      (is (thrown-with-msg?
            ExceptionInfo #"Registry failure: not-existing-addr: Name or service not known"
            (repositories {:url (str "http://not-existing-addr-" (swarmpit.uuid/uuid))})))))

  (testing "timeout error"
    (with-redefs [http/execute-in-scope (fn [_] (throw (ex-info "Registry error: Request timeout"
                                                               {:status 408
                                                                :type :http-client
                                                                :body {:error "Request timeout"}})))]
      (is (thrown-with-msg?
            ExceptionInfo #"Registry error: Request timeout"
            (repositories {:url (str "http://slow-registry-" (swarmpit.uuid/uuid))})))))

  (testing "digest accepts OCI manifest types"
    (let [captured (atom nil)]
      (with-redefs [http/execute-in-scope (fn [request]
                                            (reset! captured request)
                                            {:status  200
                                             :headers {:docker-content-digest "sha256:abc123"}
                                             :body    nil})]
        (is (= "sha256:abc123"
               (digest {:url "http://registry:5000"} "myapp" "latest")))
        (let [{:keys [method url options]} @captured
              accept (get-in options [:headers :Accept])]
          (is (= :HEAD method))
          (is (= "http://registry:5000/v2/myapp/manifests/latest" url))
          (is (.contains accept "application/vnd.oci.image.index.v1+json"))
          (is (.contains accept "application/vnd.oci.image.manifest.v1+json"))
          (is (.contains accept "application/vnd.docker.distribution.manifest.list.v2+json"))
          (is (.contains accept "application/vnd.docker.distribution.manifest.v2+json")))))))
