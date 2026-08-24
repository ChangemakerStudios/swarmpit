(ns swarmpit.docker.utils-test
  (:require [clojure.test :refer :all]
            [swarmpit.docker.utils :as du]))

;; `distribution-id` reads the registry address off an image reference and keeps
;; the port; `registry-address` reads the same value off a stored registry url.
;; The two are compared to each other when deciding which registries to log into
;; before a stack deploy, so they have to agree.

(deftest registry-address-strips-scheme
  (is (= "registry.example.com" (du/registry-address "https://registry.example.com")))
  (is (= "registry.example.com" (du/registry-address "http://registry.example.com"))))

(deftest registry-address-keeps-port
  (is (= "registry.example.com:5000"
         (du/registry-address "https://registry.example.com:5000"))))

(deftest registry-address-drops-path-and-trailing-slash
  (is (= "registry.example.com:5000"
         (du/registry-address "https://registry.example.com:5000/")))
  (is (= "registry.example.com"
         (du/registry-address "https://registry.example.com/artifactory/docker"))))

(deftest registry-address-tolerates-nil
  (is (nil? (du/registry-address nil))))

(deftest registry-address-round-trips-with-distribution-id
  (doseq [[url image] [["https://registry.example.com"      "registry.example.com/team/app:1.0"]
                       ["https://registry.example.com:5000" "registry.example.com:5000/team/app:1.0"]
                       ["http://localhost:5000"             "localhost:5000/app:latest"]]]
    (is (= (du/registry-address url) (du/distribution-id image))
        (str "registry url " url " must match the address parsed from " image))))
