(ns swarmpit.registry.client
  (:import (clojure.lang ExceptionInfo))
  (:require [clojure.walk :refer [keywordize-keys stringify-keys]]
            [clojure.string :as str]
            [cheshire.core :refer [parse-string]]
            [swarmpit.http :refer :all]
            [swarmpit.token :as token]
            [swarmpit.ip :as ip]))

(defn- build-url
  [registry api]
  (if (:customApi registry)
    (str (:url registry) api)
    (str (:url registry) "/v2" api)))

(defn- basic-auth
  [registry]
  (when (:withAuth registry)
    {:Authorization (token/generate-basic (:username registry)
                                          (:password registry))}))

(defn- authenticate-header
  [headers]
  (let [www-authenticate (:www-authenticate headers)]
    (when www-authenticate
      (keywordize-keys
        (into (sorted-map)
              (map #(str/split % #"=")
                   (-> www-authenticate
                       (str/split #" ")
                       (second)
                       (str/replace "\"" "")
                       (str/split #","))))))))

(defn- execute
  [{:keys [method url options quiet-statuses]}]
  (execute-in-scope {:method          method
                     :url             url
                     :options         (merge {:insecure? true} options)
                     :scope           "Registry"
                     :timeout         5000
                     :error-handler   #(-> % :errors (first) :message)
                     :quiet-statuses  quiet-statuses}))

(defn- repository-scope-from-url
  "Extract repository scope from Docker V2 API URL.
   GHCR returns a generic placeholder scope in www-authenticate headers,
   so we derive the correct scope from the actual request URL."
  [url]
  (when-let [[_ repo] (re-find #"/v2/(.+?)(?:/(?:tags|manifests|blobs)/)" url)]
    (str "repository:" repo ":pull")))

(defn- fallback-options
  [www-auth-url www-auth-params options]
  (let [query-params (merge {:client_id "swarmpit"} www-auth-params)
        token-options (assoc options :query-params query-params)
        token-body (-> (execute {:method  :GET
                                 :url     www-auth-url
                                 :options token-options})
                       :body)
        bearer-token (or (:token token-body) (:access_token token-body))]
    (assoc-in options [:headers :Authorization] (token/bearer bearer-token))))

(defn- execute-with-fallback
  [{:keys [method url options quiet-statuses] :as request}]
  (try
    (execute (update request :quiet-statuses (fnil conj #{}) 401))
    (catch ExceptionInfo e
      (let [status (:status (ex-data e))
            headers (:headers (ex-data e))
            www-auth-header (authenticate-header headers)
            www-auth-url (:realm www-auth-header)
            www-auth-params (dissoc www-auth-header :realm)
            url-scope (repository-scope-from-url url)
            www-auth-params (cond-> www-auth-params
                              url-scope (assoc :scope url-scope))]
        (if (and (= status 401)
                 (some? www-auth-url)
                 (ip/is-valid-url www-auth-url))
          (-> request
              (assoc :options (fallback-options www-auth-url www-auth-params options))
              (execute))
          (throw e))))))

(defn repositories
  [registry]
  (-> (execute-with-fallback
        {:method  :GET
         :url     (build-url registry "/_catalog")
         :options {:headers (basic-auth registry)}})
      :body
      :repositories))

(defn info
  [registry]
  (-> (execute-with-fallback
        {:method  :GET
         :url     (build-url registry "/")
         :options {:headers (basic-auth registry)}})
      :body))

(defn tags
  [registry repository-name]
  (-> (execute-with-fallback
        {:method  :GET
         :url     (build-url registry (str "/" repository-name "/tags/list"))
         :options {:headers (basic-auth registry)}})
      :body))

(def ^:private v1-manifest "application/vnd.docker.distribution.manifest.v1+prettyjws")
(def ^:private v2-manifest "application/vnd.docker.distribution.manifest.v2+json")
(def ^:private v2-list "application/vnd.docker.distribution.manifest.list.v2+json")
(def ^:private oci-manifest "application/vnd.oci.image.manifest.v1+json")
(def ^:private oci-index "application/vnd.oci.image.index.v1+json")

(def ^:private compatible-types
  {v2-list     #{v2-list oci-index}
   v2-manifest #{v2-manifest oci-manifest}
   v1-manifest #{v1-manifest}})

(defn- request-manifest
  [registry repository-name repository-tag method type]
  (let [response (execute-with-fallback {:method          method
                                         :url             (build-url registry (str "/" repository-name "/manifests/" repository-tag))
                                         :options         {:headers (merge (basic-auth registry)
                                                                           {:Accept type})}
                                         :quiet-statuses  #{404}})
        response-type (get-in response [:headers :content-type])
        normalized-content-type (when response-type
                                  (-> response-type str/trim (str/split #";") first str/trim str/lower-case))
        accepted-types (get compatible-types type #{type})]
    (when (contains? accepted-types normalized-content-type) response)))

(defn manifest
  [registry repository-name repository-tag]
  (:body (request-manifest registry repository-name repository-tag :GET
                           "application/vnd.docker.distribution.manifest.v1+prettyjws")))

(defn digest
  [registry repository-name repository-tag]
  (-> (or (request-manifest registry repository-name repository-tag :HEAD v2-list)
          (request-manifest registry repository-name repository-tag :HEAD v2-manifest))
      :headers
      :docker-content-digest))

(def ^:private manifest-accept
  (str/join ", " [oci-index v2-list oci-manifest v2-manifest v1-manifest]))

(defn- response-type
  [response]
  (some-> (get-in response [:headers :content-type])
          (str/trim)
          (str/split #";")
          (first)
          (str/trim)
          (str/lower-case)))

(defn- fetch-manifest
  "GET a manifest by tag or digest, accepting every type we can read. Registries
   are not obliged to honour Accept - GHCR in particular answers with whatever
   it has - so dispatch on what came back, not on what was asked for."
  [registry repository-name reference]
  (let [response (execute-with-fallback
                   {:method         :GET
                    :url            (build-url registry (str "/" repository-name "/manifests/" reference))
                    :options        {:headers (merge (basic-auth registry)
                                                     {:Accept manifest-accept})}
                    :quiet-statuses #{404}})]
    {:type (response-type response)
     :body (:body response)}))

(defn- blob
  [registry repository-name blob-digest]
  (-> (execute-with-fallback
        {:method         :GET
         :url            (build-url registry (str "/" repository-name "/blobs/" blob-digest))
         :options        {:headers (basic-auth registry)}
         :quiet-statuses #{404}})
      :body))

(defn- platform-manifest-digest
  "Which image manifest of a multi-arch index to inspect. Prefer linux/amd64,
   then any linux entry. Entries with an unknown platform are attestations and
   carry no image config."
  [index]
  (let [entries (->> (:manifests index)
                     (remove #(= "unknown" (get-in % [:platform :architecture]))))]
    (->> [(first (filter #(and (= "linux" (get-in % [:platform :os]))
                               (= "amd64" (get-in % [:platform :architecture]))) entries))
          (first (filter #(= "linux" (get-in % [:platform :os])) entries))
          (first entries)]
         (keep identity)
         (first)
         :digest)))

(defn repository-config
  "Image config for repository:reference, shaped as the image mappers expect
   ({:config {:ExposedPorts ...}}).

   Schema 1 inlines the config in the manifest. Schema 2 and OCI keep it in a
   separate blob, and a multi-arch tag resolves to an index that has to be
   followed to a platform manifest first. Returns nil when the config cannot be
   reached, so a registry that will not surrender it costs us the port
   suggestions and nothing else."
  ([registry repository-name reference]
   (repository-config registry repository-name reference 0))
  ([registry repository-name reference depth]
   (when (< depth 2)
     (try
       (let [{:keys [type body]} (fetch-manifest registry repository-name reference)]
         (cond
           (= v1-manifest type)
           (some-> body :history (first) :v1Compatibility (parse-string true))

           (contains? #{v2-list oci-index} type)
           (when-let [manifest-digest (platform-manifest-digest body)]
             (repository-config registry repository-name manifest-digest (inc depth)))

           (contains? #{v2-manifest oci-manifest} type)
           (when-let [config-digest (get-in body [:config :digest])]
             (blob registry repository-name config-digest))

           :else nil))
       (catch ExceptionInfo _ nil)))))
