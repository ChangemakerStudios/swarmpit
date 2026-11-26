(ns swarmpit.docker.registry.client-test
  (:require [clojure.test :refer :all]
            [swarmpit.docker.auth.client :as auth]
            [swarmpit.docker.registry.client :refer :all])
  (:import (clojure.lang ExceptionInfo)))

(deftest ^:integration docker-registry-test

  (testing "error"
    (is (thrown-with-msg?
          ExceptionInfo #"Docker registry error: authentication required"
          (tags nil "nginx")))))

(defn- hub-digest
  [repository tag]
  (-> (auth/token nil repository) :token (digest repository tag)))

(deftest ^:integration oci-manifest-digest-test
  (testing "images published as an OCI index still resolve a digest (#738)"
    (doseq [repository ["library/nginx" "library/alpine" "library/traefik"]]
      (is (re-matches #"sha256:[0-9a-f]{64}" (str (hub-digest repository "latest")))
          (str repository " must resolve, else autoredeploy force-updates it every poll"))))

  (testing "images published as a docker manifest list keep working"
    (is (re-matches #"sha256:[0-9a-f]{64}" (str (hub-digest "grafana/grafana" "latest"))))))

(defn- manifest-with
  "Call the private request-manifest against a stubbed registry answering with
   the given content type; returns [accept-header-sent response-or-nil]."
  [type response-content-type]
  (let [sent (atom nil)]
    (with-redefs [swarmpit.http/execute-in-scope
                  (fn [{:keys [options]}]
                    (reset! sent (get-in options [:headers :Accept]))
                    {:status 200 :headers {:content-type response-content-type} :body {}})]
      (let [response (#'swarmpit.docker.registry.client/request-manifest "t" "acme/app" "latest" :GET type)]
        [@sent response]))))

(def ^:private list-v2 "application/vnd.docker.distribution.manifest.list.v2+json")
(def ^:private oci-index "application/vnd.oci.image.index.v1+json")

(deftest request-manifest-content-types
  (testing "the Accept header offers every type compatible with the one asked for"
    (let [[accept _] (manifest-with list-v2 list-v2)]
      (is (= #{list-v2 oci-index} (set (clojure.string/split accept #", "))))))

  (testing "a compatible OCI answer is accepted"
    (is (some? (second (manifest-with list-v2 oci-index)))))

  (testing "parameters and case in Content-Type are normalized"
    (is (some? (second (manifest-with list-v2 "Application/VND.OCI.Image.Index.v1+JSON; charset=utf-8")))))

  (testing "an incompatible type is rejected"
    (is (nil? (second (manifest-with list-v2 "application/vnd.docker.distribution.manifest.v1+prettyjws"))))
    (is (nil? (second (manifest-with list-v2 nil))))))
