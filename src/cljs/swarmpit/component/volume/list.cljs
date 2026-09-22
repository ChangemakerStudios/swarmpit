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

(defn- tree-rows
  "Collapse volumes sharing a prefix (text before the first `_`) under one header row.
   A prefix needs two members to become a group; anonymous volumes always group."
  [items expanded force-open?]
  (let [groups (group-by (comp group-key :volumeName) items)
        grouped? (fn [k] (and k (or (= ::anonymous k) (< 1 (count (get groups k))))))]
    (->> items
         (map #(let [k (group-key (:volumeName %))]
                 (if (grouped? k) {:group k} %)))
         (distinct)
         (mapcat (fn [{k :group :as entry}]
                   (if k
                     (let [members (get groups k)
                           open? (or force-open? (contains? expanded k))]
                       (cons {:group k :count (count members) :open? open?}
                             (when open? (map #(assoc % :child? true) members))))
                     [entry]))))))

(defn- group-label
  [{k :group n :count :keys [open?]}]
  (html
    [:span.Swarmpit-tree-group
     {:onClick (fn [e]
                 (.preventDefault e)
                 (.stopPropagation e)
                 (toggle-group! k))}
     (if open? (icon/expand-more) (icon/chevron-right))
     [:span (if (= ::anonymous k) "anonymous" k)]
     [:span.Swarmpit-tree-count n]]))

(defn- render-name
  [{:keys [volumeName child?] :as item}]
  (if (:group item)
    (group-label item)
    (let [label (if (anonymous? volumeName)
                  (html [:span.Swarmpit-volume-anonymous volumeName])
                  volumeName)]
      (if child?
        (html [:span.Swarmpit-tree-child label])
        label))))

(def render-metadata
  {:table {:summary [{:name      "Name"
                      :render-fn render-name}
                     {:name      "Driver"
                      :render-fn (fn [item] (:driver item))}]}
   :list  {:primary   render-name
           :secondary (fn [item] (:driver item))}})

(defn onclick-handler
  [item]
  (when-not (:group item)
    (routes/path-for-frontend :volume-info {:name (:volumeName item)})))

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
    (storage/admin?) (conj {:name    "Prune…"
                            :variant "outlined"
                            :onClick #(dispatch! (routes/path-for-frontend :maintenance))})
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
                            (sort-by (juxt (comp anonymous? :volumeName) :volumeName)))
        searching? (<= 2 (count query))]
    (progress/form
      loading?
      (common/list-rows "Volumes"
                        items
                        filtered-items
                        (if tree?
                          (tree-rows filtered-items (or expanded #{}) searching?)
                          filtered-items)
                        render-metadata
                        onclick-handler
                        (toolbar-render-metadata tree?)))))
