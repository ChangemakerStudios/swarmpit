(ns swarmpit.github.client-test
  (:require [clojure.test :refer :all]
            [swarmpit.github.client :refer :all]
            [swarmpit.api :as api]
            [swarmpit.http :as http]))

(def ^:private registry {:username "someone" :token "gh-token"})

(defn- package
  [owner name]
  {:name name :package_type "container" :owner {:login owner}})

(deftest user-packages-test
  (testing "single page is returned as-is"
    (with-redefs [http/execute-in-scope (fn [_] {:body [(package "someone" "app")]})]
      (is (= [(package "someone" "app")]
             (user-packages registry)))))

  (testing "a full page triggers a follow-up request until a short page arrives"
    (let [full-page (mapv #(package "someone" (str "app-" %)) (range 100))
          requested (atom [])]
      (with-redefs [http/execute-in-scope
                    (fn [{:keys [options]}]
                      (let [page (get-in options [:query-params :page])]
                        (swap! requested conj page)
                        {:body (if (= 1 page) full-page [(package "someone" "last")])}))]
        (is (= 101 (count (user-packages registry))))
        (is (= [1 2] @requested)))))

  (testing "container packages are requested, not every package type"
    (with-redefs [http/execute-in-scope
                  (fn [{:keys [options]}]
                    (is (= "container" (get-in options [:query-params :package_type])))
                    {:body []})]
      (is (= [] (user-packages registry))))))

(deftest org-packages-test
  (testing "an org the token cannot read yields no packages instead of failing"
    (with-redefs [http/execute-in-scope (fn [_] (throw (ex-info "Github error: Not Found"
                                                                {:status 404
                                                                 :type   :http-client
                                                                 :body   {:error "Not Found"}})))]
      (is (= [] (org-packages registry "some-org"))))))

(deftest user-organizations-test
  (testing "a token without read:org yields no orgs instead of failing"
    (with-redefs [http/execute-in-scope (fn [_] (throw (ex-info "Github error: Requires authentication"
                                                                {:status 401
                                                                 :type   :http-client
                                                                 :body   {:error "Requires authentication"}})))]
      (is (= [] (user-organizations registry))))))

(deftest ghcr-repository-mapping-test
  (let [->repo #'api/->ghcr-repository]
    (testing "repository path is owner/name"
      (is (= "someone/app" (:name (->repo (package "someone" "app"))))))

    (testing "mixed-case owner logins are downcased so the image is pullable"
      (is (= "changemakerstudios/swarmpit"
             (:name (->repo (package "ChangemakerStudios" "swarmpit"))))))

    (testing "id is stable for a given path"
      (is (= (:id (->repo (package "ChangemakerStudios" "swarmpit")))
             (:id (->repo (package "changemakerstudios" "swarmpit"))))))))
