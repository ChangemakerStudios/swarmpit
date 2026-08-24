(ns swarmpit.base64-test
  (:require [clojure.test :refer :all]
            [cheshire.core :refer [generate-string]]
            [swarmpit.base64 :as base64])
  (:import (java.util Base64)))

;; Docker decodes the X-Registry-Auth header with Go's base64.URLEncoding, which
;; rejects '+' and '/'. When it fails it only logs "invalid authconfig" and then
;; pulls anonymously, so the registry answers "no basic auth credentials" and the
;; swarm rejects the task - with nothing to show for it on the Swarmpit side.

(def ^:private auth-with-unsafe-chars
  ;; '?' at this offset lands in the last 6 bits of a base64 group, which is
  ;; where the standard alphabet emits '/'.
  {:username "jaben" :password "abc?def" :serveraddress "https://registry.example.com"})

(deftest encode-url-never-emits-standard-alphabet-chars
  (let [payload (generate-string auth-with-unsafe-chars)]
    (is (re-find #"[+/]" (base64/encode payload))
        "sanity: this payload must be one that standard base64 encodes unsafely")
    (is (nil? (re-find #"[+/]" (base64/encode-url payload)))
        "base64url output must contain neither '+' nor '/'")))

(deftest encode-url-round-trips-through-go-compatible-decoder
  (let [payload (generate-string auth-with-unsafe-chars)
        decoded (String. (.decode (Base64/getUrlDecoder) (base64/encode-url payload)))]
    (is (= payload decoded))))

(deftest encode-url-keeps-padding
  ;; Go's base64.URLEncoding is the padded variant; RawURLEncoding is not
  ;; interchangeable with it, so the '=' padding has to survive.
  (is (= (mod (count (base64/encode-url "abcd")) 4) 0)))
