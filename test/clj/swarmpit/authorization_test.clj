(ns swarmpit.authorization-test
  (:require [clojure.test :refer :all]
            [swarmpit.authorization :as authorization]
            [swarmpit.config :as cfg]))

(def ^:private event-push-access #'authorization/event-push-access)

(defn- allowed?
  [result]
  (true? result))

(defn- push-request
  ([] (push-request nil))
  ([query-string] {:request-method :post
                   :uri            "/events"
                   :query-string   query-string}))

(deftest event-push-access-test
  (testing "with no token configured the stock agent is allowed through"
    (with-redefs [cfg/config (fn [_] nil)]
      (is (allowed? (event-push-access (push-request))))))

  (testing "a blank token is treated as unset, not as a token of \"\""
    (with-redefs [cfg/config (fn [_] "   ")]
      (is (allowed? (event-push-access (push-request))))))

  (testing "an empty EVENT_ENDPOINT interpolation does not unlock a real token"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (not (allowed? (event-push-access (push-request "token=")))))))

  (testing "with a token configured the matching query param is allowed"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (allowed? (event-push-access (push-request "token=s3cret"))))))

  (testing "the matching X-Swarmpit-Event-Token header is allowed"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (allowed? (event-push-access
                      (assoc (push-request)
                        :headers {"x-swarmpit-event-token" "s3cret"}))))))

  (testing "a wrong header token is rejected"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (not (allowed? (event-push-access
                           (assoc (push-request)
                             :headers {"x-swarmpit-event-token" "nope"})))))))

  (testing "the token is found among other query params"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (allowed? (event-push-access (push-request "foo=bar&token=s3cret"))))))

  (testing "a wrong token is rejected"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (not (allowed? (event-push-access (push-request "token=nope")))))))

  (testing "a missing token is rejected once one is configured"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (not (allowed? (event-push-access (push-request)))))))

  (testing "an authenticated user is allowed regardless of token"
    (with-redefs [cfg/config (fn [_] "s3cret")]
      (is (allowed? (event-push-access (assoc (push-request)
                                         :identity {:usr {:username "admin"}})))))))

(defn- with-token
  [token f]
  (try
    ;; pin both names so a SWARMPIT_EVENT_TOKEN in the environment can't leak in
    (cfg/update! {:event-token nil :agent-token token})
    (f)
    (finally (cfg/update! {}))))

(def ^:private handled {:status 200 :body "handled"})

(def ^:private app
  (authorization/authorization-middleware (constantly handled)))

(defn- push-event
  [request]
  (app (merge {:request-method :post
               :uri            "/events"
               :remote-addr    "10.0.0.5"}
              request)))

(defn- accepted?
  [request]
  (= handled (push-event request)))

(deftest event-push-without-a-configured-token
  (testing "released agents send no credentials, so the push is accepted (#735)"
    (with-token nil
      #(is (accepted? {})))))

(deftest event-push-with-a-configured-token
  (with-token "s3cret"
    (fn []
      (testing "accepted via header"
        (is (accepted? {:headers {"x-swarmpit-agent-token" "s3cret"}})))

      (testing "accepted via query string, which is all EVENT_ENDPOINT can carry"
        (is (accepted? {:query-string "token=s3cret"}))
        (is (accepted? {:query-string "other=1&token=s3cret"})))

      (testing "rejected when missing"
        (is (not (accepted? {})))
        (is (not (accepted? {:query-string ""}))))

      (testing "rejected when wrong"
        (is (not (accepted? {:headers {"x-swarmpit-agent-token" "nope"}})))
        (is (not (accepted? {:query-string "token=nope"}))))

      (testing "a malformed query string is rejected, not a crash"
        (is (not (accepted? {:query-string "%%%"})))
        (is (not (accepted? {:query-string "token=%ZZ"}))))

      (testing "rejection is a tagged 401"
        (let [{:keys [status headers body]} (push-event {})]
          (is (= 401 status))
          (is (= "swarmpit" (get headers "X-Backend-Server")))
          (is (= "Authentication failed" (:error body)))))

      (testing "an authenticated user is still allowed through"
        (is (accepted? {:identity {:usr {:username "admin"}}}))))))

(deftest event-push-token-is-url-decoded
  (with-token "s3/cret"
    (fn []
      (is (accepted? {:query-string "token=s3%2Fcret"}))
      (is (not (accepted? {:query-string "token=s3%2Fnope"}))))))

(deftest event-push-prefers-event-token-over-agent-token
  (try
    (cfg/update! {:event-token "ours" :agent-token "theirs"})
    (is (accepted? {:headers {"x-swarmpit-event-token" "ours"}}))
    (is (accepted? {:headers {"x-swarmpit-agent-token" "ours"}}))
    (is (not (accepted? {:headers {"x-swarmpit-agent-token" "theirs"}})))
    (finally (cfg/update! {}))))

(deftest event-stream-stays-open-for-the-browser
  (testing "GET /events is not gated by the agent token"
    (with-token "s3cret"
      #(is (= handled (app {:request-method :get :uri "/events"}))))))
