(ns swarmpit.api-test
  (:require [clojure.test :refer :all]
            [digest :refer [digest]]
            [swarmpit.api :refer :all]
            [swarmpit.config :as cfg]
            [swarmpit.yaml :as yaml]
            [swarmpit.couchdb.mapper.outbound :refer [->password]]))

(deftest password-check-test
  (let [pass "heslo"
        hashed (->password pass)]

    (testing "speed"
      (cfg/update! {:password-hashing
                    {:alg :pbkdf2+sha512 :iterations 100000}})
      (println "Evaluating baseline" (cfg/config :password-hashing))
      (time (is (some? (->password pass))))
      (cfg/update! {})
      (println "Evaluating defaults" (cfg/config :password-hashing))
      (time (is (some? (->password pass)))))

    (testing "check"
      (is (true? (password-check pass hashed))))

    (testing "upgrade from old hash"
      (let [old-hash (digest "sha-256" pass)
            new-hash (atom nil)]
        (is (thrown? Exception (password-check pass old-hash)))
        (is (false? (password-check-upgrade pass "heslo" nil)))
        (is (true? (password-check-upgrade pass old-hash
                                           #(reset! new-hash hashed))))
        (is (some? @new-hash))
        (is (true? (password-check pass @new-hash)))
        (is (true? (password-check-upgrade pass @new-hash nil)))))))

(deftest merge-stackfile-service-test
  (let [stored (yaml/->json "version: '3.8'
services:
  app:
    image: app:1.0
    environment:
      PRICE: $$5
  db:
    image: postgres:16
networks:
  backend:
    driver: overlay
    attachable: true
")
        live {:services {:app {:image       "app:2.0"
                               :environment {:PRICE "$5"}
                               :networks    ["backend" "proxy"]}}
              :networks {:backend {:driver "overlay"}
                         :proxy   {:external true}}}
        merged (merge-stackfile-service stored live)]

    (testing "edited service replaced, $ re-escaped for stack deploy"
      (is (= {:image       "app:2.0"
              :environment {:PRICE "$$5"}
              :networks    ["backend" "proxy"]}
             (get-in merged [:services :app]))))

    (testing "other services and top-level keys untouched, order kept"
      (is (= "3.8" (:version merged)))
      (is (= "postgres:16" (get-in merged [:services :db :image])))
      (is (= [:version :services :networks] (keys merged)))
      (is (= [:app :db] (keys (:services merged)))))

    (testing "existing resources kept as written, new ones added"
      (is (= {:driver "overlay" :attachable true} (get-in merged [:networks :backend])))
      (is (= {:external true} (get-in merged [:networks :proxy]))))

    (testing "round-trips through yaml"
      (is (= merged (yaml/->json (yaml/->yaml merged)))))))
