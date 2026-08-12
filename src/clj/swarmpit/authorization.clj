(ns swarmpit.authorization
  (:require [buddy.auth :refer [authenticated?]]
            [buddy.auth.accessrules :refer [success error wrap-access-rules]]
            [clojure.string :as str]
            [swarmpit.config :as cfg]
            [swarmpit.handler :refer [resp-error]]
            [swarmpit.token :refer [admin?]]
            [swarmpit.token :refer [user?]]
            [swarmpit.couchdb.client :as cc]))

(defn- authenticated-access
  [request]
  (if (authenticated? request)
    true
    (error {:code    401
            :message "Authentication failed"})))

(defn- any-access
  [_]
  true)

(defn- query-param
  "Read a raw query parameter. Access rules run before the parameters
   middleware, so the query string has not been parsed yet."
  [request param]
  (some->> (str/split (or (:query-string request) "") #"&")
           (map #(str/split % #"=" 2))
           (filter #(= param (first %)))
           (first)
           (second)))

(defn- event-push-access
  "Authorize the agent's `POST /events`.

   The agent ships no credentials - it simply posts to EVENT_ENDPOINT - so
   requiring a login token here silently kills stats collection for every
   default deployment. Instead:

     - SWARMPIT_EVENT_TOKEN unset (default): the endpoint stays open, so the
       stock agent works out of the box exactly as it always has.
     - SWARMPIT_EVENT_TOKEN set: the agent must present it, either as the
       X-Swarmpit-Event-Token header (agent 2.3.0+) or as a `token` query
       parameter, which any agent can send because EVENT_ENDPOINT is a full URL:
       EVENT_ENDPOINT=http://app:8080/events?token=<token>

   The secret deliberately does not travel in the Authorization header: that is
   parsed as a JWT by the authentication middleware, which runs first and
   rejects anything it cannot verify before these rules are consulted.

   A logged-in user is always allowed, which keeps the endpoint usable from the
   UI and from tests."
  [request]
  (let [expected (cfg/config :event-token)]
    (cond
      (str/blank? expected) true
      (= expected (get-in request [:headers "x-swarmpit-event-token"])) true
      (= expected (query-param request "token")) true
      (authenticated? request) true
      :else (error {:code    401
                    :message "Authentication failed"}))))

(defn- admin-access
  [{:keys [identity]}]
  (let [username (get-in identity [:usr :username])
        user (cc/user-by-username username)]
    (if (admin? user)
      true
      (error {:code    403
              :message "Unauthorized admin access"}))))

(defn- user-access
  [{:keys [identity]}]
  (let [username (get-in identity [:usr :username])
        user (cc/user-by-username username)]
    (if (or (admin? user) (user? user))
      true
      (error {:code    403
              :message "Unauthorized user access"}))))

(defn- owner-access
  [{:keys [path-params identity]}]
  (let [user (get-in identity [:usr :username])
        entity (cc/get-doc (:id path-params))]
    (if (= (:owner entity) user)
      true
      (error {:code    403
              :message "Unauthorized owner access"}))))

(defn- registry-access
  [{:keys [path-params identity]}]
  (let [user (get-in identity [:usr :username])
        entity (cc/get-doc (:id path-params))]
    (if (or (= (:owner entity) user)
            (:public entity))
      true
      (error {:code    403
              :message "Unauthorized registry access"}))))

(def ^:private registry-types "dockerhub|v2|ecr|acr|gitlab|ghcr")

(defn- registry-pattern [suffix]
  (re-pattern (str "^/api/registry/(" registry-types ")/[a-zA-Z0-9]*" suffix)))

(def rules [{:pattern #"^/login$"
             :handler any-access}
            {:pattern        #"^/events$"
             :request-method :get
             :handler        any-access}
            {:pattern        #"^/events$"
             :request-method :post
             :handler        event-push-access}
            {:pattern #"^/version$"
             :handler any-access}
            {:pattern #"^/initialize$"
             :handler any-access}
            {:pattern #"^/logout$"
             :handler authenticated-access}
            {:pattern #"^/slt"
             :handler authenticated-access}
            {:pattern #"^/api/swagger.json"
             :handler any-access}
            {:pattern #"^/api/admin/.*"
             :handler {:and [authenticated-access admin-access]}}
            {:pattern #"^/$"
             :handler any-access}
            {:pattern        #"^/api/nodes/[a-zA-Z0-9]*$"
             :request-method #{:delete :post}
             :handler        {:and [authenticated-access admin-access]}}
            {:pattern        (registry-pattern "/repositories$")
             :request-method :get
             :handler        {:and [authenticated-access registry-access user-access]}}
            {:pattern        (registry-pattern "/tags$")
             :request-method :get
             :handler        {:and [authenticated-access registry-access user-access]}}
            {:pattern        (registry-pattern "/ports$")
             :request-method :get
             :handler        {:and [authenticated-access registry-access user-access]}}
            {:pattern        (registry-pattern "$")
             :request-method #{:get :delete :post}
             :handler        {:and [authenticated-access owner-access user-access]}}
            {:pattern        #"^/api/.*/dashboard$"
             :request-method #{:delete :post}
             :handler        {:and [authenticated-access]}} ;;Allow pin/unpin by authenticated
            {:pattern        #"^/api/.*"
             :request-method #{:delete :post}
             :handler        {:and [authenticated-access user-access]}} ;;Restrict ALL delete/post to user level and higher
            {:pattern #"^/api/secrets/$"
             :handler {:and [authenticated-access user-access]}} ;;Restrict getting secrets to user level and higher
            {:pattern #"^/api/secrets/.*$"
             :handler {:and [authenticated-access user-access]}} ;;Restrict getting secrets to user level and higher
            {:pattern #"^/api/.*"
             :handler authenticated-access}])

(defn- rules-error
  [_ val]
  (-> (resp-error (:code val)
                  (:message val))
      (assoc :headers {"X-Backend-Server" "swarmpit"})))

(defn authorization-middleware
  [handler]
  (wrap-access-rules handler {:rules    rules
                              :on-error rules-error}))
