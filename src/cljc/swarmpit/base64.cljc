(ns swarmpit.base64
  #?(:clj  (:import java.util.Base64)
     :cljs (:require [goog.crypt :as crypt]
                     [goog.crypt.base64 :as b64])))

#?(:clj
   (defn encode
     [data]
     (let [credentials-bytes (.getBytes (str data))]
       (.encodeToString (Base64/getEncoder) credentials-bytes)))
   :cljs
   (defn encode
     [data]
     (-> (crypt/stringToUtf8ByteArray data)
         (b64/encodeByteArray))))

#?(:clj
   (defn encode-url
     "base64url per RFC 4648 section 5. Docker decodes the X-Registry-Auth
      header with Go's base64.URLEncoding, which rejects the '+' and '/' of the
      standard alphabet. A blob containing either decodes to an EMPTY auth
      config - the daemon only logs a warning and then pulls anonymously, so a
      private registry answers the challenge with \"no basic auth credentials\"
      as though no account were linked at all."
     [data]
     (let [data-bytes (.getBytes (str data))]
       (.encodeToString (Base64/getUrlEncoder) data-bytes))))

#?(:clj
   (defn decode
     [encoded-data]
     (String. (.decode (Base64/getDecoder)
                       encoded-data)))
   :cljs
   (defn decode
     [encoded-data]
     (-> (b64/decodeStringToByteArray encoded-data)
         (crypt/utf8ByteArrayToString))))
