(ns swarmpit.github.client
  (:import (clojure.lang ExceptionInfo))
  (:require [swarmpit.http :refer :all]))

(def ^:private api-url "https://api.github.com")

(def ^:private per-page 100)

;; Guard against a runaway pagination loop on accounts with many packages.
(def ^:private max-pages 20)

(defn- execute
  [{:keys [method api options]}]
  (execute-in-scope {:method        method
                     :url           (str api-url api)
                     :options       (merge {:insecure? true} options)
                     :scope         "Github"
                     :error-handler #(or (:message %) (:error %))}))

(defn- auth-options
  [registry]
  {:headers {:Authorization        (str "Bearer " (:token registry))
             :Accept               "application/vnd.github+json"
             :X-GitHub-Api-Version "2022-11-28"}})

(defn- fetch-paged
  "GET every page of a GitHub list endpoint. GitHub signals the last page by
   returning fewer items than requested."
  [registry api query-params]
  (loop [page 1
         acc  []]
    (let [items (-> (execute {:method  :GET
                              :api     api
                              :options (assoc (auth-options registry)
                                         :query-params (merge query-params
                                                              {:per_page per-page
                                                               :page     page}))})
                    :body)
          acc   (into acc items)]
      (if (and (= per-page (count items))
               (< page max-pages))
        (recur (inc page) acc)
        acc))))

(defn user-packages
  "Container packages owned by the authenticated user. Requires read:packages."
  [registry]
  (fetch-paged registry "/user/packages" {:package_type "container"}))

(defn user-organizations
  "Organizations visible to the authenticated user. A token without read:org
   simply sees fewer orgs, so treat a failure as 'no orgs' rather than fatal."
  [registry]
  (try
    (fetch-paged registry "/user/orgs" {})
    (catch ExceptionInfo _
      [])))

(defn org-packages
  "Container packages owned by an organization. The token may not carry access
   to every org the user belongs to, so a failure here must not sink the
   whole listing."
  [registry org]
  (try
    (fetch-paged registry (str "/orgs/" org "/packages") {:package_type "container"})
    (catch ExceptionInfo _
      [])))
