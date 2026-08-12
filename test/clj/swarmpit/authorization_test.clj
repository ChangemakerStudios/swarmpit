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
