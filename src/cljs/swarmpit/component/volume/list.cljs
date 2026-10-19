(ns swarmpit.component.volume.list
  (:require [material.icon :as icon]
            [material.components :as comp]
            [material.component.list.basic :as list]
            [material.component.list.util :as list-util]
            [swarmpit.component.common :as common]
            [swarmpit.component.state :as state]
            [swarmpit.component.mixin :as mixin]
            [swarmpit.component.progress :as progress]
            [swarmpit.ajax :as ajax]
            [swarmpit.routes :as routes]
            [swarmpit.url :refer [dispatch!]]
            [sablono.core :refer-macros [html]]
            [rum.core :as rum]
            [clojure.string :as str]
            [swarmpit.storage :as storage]))

(enable-console-print!)

(def ^:private tree-storage-key "volumeTreeView")

(defn- anonymous?
  [volume-name]
  (some? (re-matches #"[0-9a-f]{64}" volume-name)))

(defn- group-key
  [volume-name]
  (if (anonymous? volume-name)
    ::anonymous
    (let [i (str/index-of volume-name "_")]
      (when (and i (pos? i))
        (subs volume-name 0 i)))))

(defn- toggle-group!
  [k]
  (state/update-value [:expanded]
                      (let [expanded (or (:expanded (state/get-value state/form-state-cursor)) #{})]
                        (if (contains? expanded k) (disj expanded k) (conj expanded k)))
                      state/form-state-cursor))

(defn- toggle-tree!
  []
  (let [tree? (not (:tree? (state/get-value state/form-state-cursor)))]
    (storage/add tree-storage-key (str tree?))
    (state/update-value [:tree?] tree? state/form-state-cursor)))

(defn- copies
  "Copies of one name - per-node local volumes, plus a shared one - collapse under
   a single row; a lone volume stays as it is. items must be sorted by name."
  [items expanded force-open?]
  (->> (partition-by :volumeName items)
       (mapcat (fn [[v :as vs]]
                 (if (= 1 (count vs))
                   vs
                   (let [k [::copies (:volumeName v)]
                         open? (or force-open? (contains? expanded k))]
                     (cons {:group k :volumeName (:volumeName v) :copies vs :open? open?}
                           (when open? (map #(assoc % :copy? true) vs)))))))))

(defn- tree-rows
  "Collapse names sharing a prefix (text before the first `_`) under one header row.
   A prefix needs two distinct names to become a group; anonymous volumes always group."
  [items expanded force-open?]
  (let [names (group-by group-key (distinct (map :volumeName items)))
        grouped? (fn [k] (and k (or (= ::anonymous k) (< 1 (count (get names k))))))]
    (->> items
         (partition-by #(let [k (group-key (:volumeName %))]
                          (if (grouped? k) k [::name (:volumeName %)])))
         (mapcat (fn [[v :as part]]
                   (let [k (group-key (:volumeName v))]
                     (if (grouped? k)
                       (let [open? (or force-open? (contains? expanded k))]
                         (cons {:group k :count (count (get names k)) :open? open?}
                               (when open? (map #(assoc % :child? true) (copies part expanded force-open?)))))
                       (copies part expanded force-open?))))))))

(defn- depth-class
  [{:keys [child? copy?]}]
  (case (+ (if child? 1 0) (if copy? 1 0))
    0 nil
    1 "Swarmpit-tree-depth-1"
    2 "Swarmpit-tree-depth-2"))

(defn- tag
  ([text] (tag text nil nil))
  ([text kind title]
   [:span {:className (str "Swarmpit-tag" (when kind (str " Swarmpit-tag-" kind)))
           :title     title}
    text]))

(def ^:private shadows-title
  "A task placed on this node mounts this local copy instead of the shared volume")

(defn- toggle-label
  [k open? & content]
  (into [:span.Swarmpit-tree-group
         {:onClick (fn [e]
                     (.preventDefault e)
                     (.stopPropagation e)
                     (toggle-group! k))}
         (if open? (icon/expand-more) (icon/chevron-right))]
        content))

(defn- render-name
  [{:keys [volumeName copies open? copy?] k :group n :count :as item}]
  (html
    [:span {:className (depth-class item)}
     (cond
       copies (toggle-label k open?
                            [:span.Swarmpit-tree-name volumeName]
                            [:span.Swarmpit-tree-count (count copies) " copies"])
       k (toggle-label k open?
                       [:span (if (= ::anonymous k) "anonymous" k)]
                       [:span.Swarmpit-tree-count n])
       (anonymous? volumeName) [:span.Swarmpit-volume-anonymous volumeName]
       copy? [:span.Swarmpit-muted volumeName]
       :else volumeName)]))

(defn- render-driver
  [{:keys [driver cluster copies]}]
  (html
    (if copies
      [:span (->> copies (map :driver) (distinct) (str/join ", "))]
      [:span.Swarmpit-tag-host driver (when cluster (tag "cluster"))])))

(defn- copies-nodes
  "Summary only - the expanded rows carry the detail."
  [copies]
  (let [nodes (keep :nodeName copies)
        on-nodes (if (< 2 (count nodes)) (str (count nodes) " nodes") (str/join ", " nodes))]
    (if (some (complement :node) copies)
      (str "shared + " on-nodes)
      on-nodes)))

(defn- render-node
  [{:keys [group node nodeName shadows copies]}]
  (html
    (cond
      copies [:span.Swarmpit-tag-host (copies-nodes copies)
              (when (some :shadows copies) (tag "shadows" "warning" shadows-title))]
      group nil
      (not node) [:span.Swarmpit-muted "all nodes"]
      :else [:span.Swarmpit-tag-host nodeName (when shadows (tag "shadows" "warning" shadows-title))])))

(def render-metadata
  {:table {:summary [{:name      "Name"
                      :render-fn render-name}
                     {:name      "Driver"
                      :render-fn render-driver}
                     {:name      "Node"
                      :render-fn render-node}]}
   :list  {:primary   render-name
           :secondary (fn [{:keys [group copies driver cluster node nodeName shadows]}]
                        (cond
                          copies (copies-nodes copies)
                          group nil
                          :else (str driver (when cluster " (cluster)") " · "
                                     (if node nodeName "all nodes")
                                     (when shadows " · shadows shared volume"))))}})

(defn onclick-handler
  [item]
  (when-not (:group item)
    (routes/path-for-frontend :volume-info
                              {:name (:volumeName item)}
                              (cond-> nil
                                (:cluster item) (assoc :cluster true)
                                (:node item) (assoc :node (:node item))))))

(defn- volumes-handler
  []
  (ajax/get
    (routes/path-for-backend :volumes)
    {:state      [:loading?]
     :on-success (fn [{:keys [response origin?]}]
                   (when origin?
                     (state/update-value [:items] response state/form-value-cursor)))}))

(defn form-search-fn
  [event]
  (state/update-value [:query] (-> event .-target .-value) state/search-cursor))

(defn- init-form-state
  []
  (state/set-value {:loading? false
                    :tree?    (= "true" (storage/get tree-storage-key))
                    :expanded #{}} state/form-state-cursor))

(def mixin-init-form
  (mixin/init-form
    (fn [_]
      (init-form-state)
      (volumes-handler))))

(defn- toolbar-render-metadata
  [tree?]
  (cond-> [{:name    (if tree? "Flat view" "Group by prefix")
            :variant "outlined"
            :onClick toggle-tree!}]
    (storage/user?) (conj {:name     "New volume"
                           :onClick  #(dispatch! (routes/path-for-frontend :volume-create))
                           :primary  true
                           :icon     (icon/add-circle-out)
                           :icon-alt (icon/add)})))

(rum/defc form < rum/reactive
                 mixin-init-form
                 mixin/subscribe-form
                 mixin/focus-filter [_]
  (let [{:keys [items]} (state/react state/form-value-cursor)
        {:keys [loading? tree? expanded]} (state/react state/form-state-cursor)
        {:keys [query]} (state/react state/search-cursor)
        ;; anonymous volumes are unreadable hashes; keep them out of the way
        filtered-items (->> (list-util/filter items query)
                            (sort-by (juxt (comp anonymous? :volumeName) :volumeName (comp some? :node) :nodeName)))
        searching? (<= 2 (count query))]
    (progress/form
      loading?
      (common/list-rows "Volumes"
                        items
                        filtered-items
                        (if tree?
                          (tree-rows filtered-items (or expanded #{}) searching?)
                          (copies filtered-items (or expanded #{}) searching?))
                        render-metadata
                        onclick-handler
                        (toolbar-render-metadata tree?)))))
