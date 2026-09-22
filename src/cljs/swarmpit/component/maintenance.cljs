(ns swarmpit.component.maintenance
  (:require [material.components :as comp]
            [swarmpit.component.state :as state]
            [swarmpit.component.mixin :as mixin]
            [swarmpit.component.message :as message]
            [swarmpit.component.progress :as progress]
            [swarmpit.ajax :as ajax]
            [swarmpit.routes :as routes]
            [swarmpit.time :as time]
            [clojure.string :as str]
            [sablono.core :refer-macros [html]]
            [rum.core :as rum]))

(enable-console-print!)

(def ^:private prunes
  {"volumes" {:title       "Prune volumes"
              :description "Removes volumes that no container is using, on every node. By default only anonymous volumes (the unnamed hash ones images create for their VOLUME paths) are removed."
              :options     [{:key     :all
                             :label   "Also remove unused named volumes"
                             :warning "Named volumes of stacks that are scaled to 0 or removed count as unused. Their data is deleted permanently."}]
              :removes     (fn [{:keys [all]}]
                             [(if all
                                "Every volume not used by a container, named ones included"
                                "Anonymous volumes not used by a container")])}
   "images"  {:title       "Prune images"
              :description "Removes dangling images (untagged layers left behind when a tag moves to a new build), on every node."
              :options     [{:key     :all
                             :label   "Remove all images without a container"
                             :warning "Every image not used by a container is deleted, so the next deploy, scale-up or restart of those services pulls them again."}]
              :removes     (fn [{:keys [all]}]
                             [(if all
                                "Every image not used by a container, tagged ones included"
                                "Dangling (untagged) images")])}
   "system"  {:title       "Prune system"
              :description "Removes stopped containers, unused networks, dangling images and build cache, on every node."
              :options     [{:key     :images
                             :label   "Remove all unused images, not just dangling ones"
                             :warning "Every image without a running container is deleted, so the next deploy or restart of those services pulls them again."}
                            {:key     :volumes
                             :label   "Also remove unused anonymous volumes"
                             :warning "Data in unused anonymous volumes is deleted permanently."}]
              :removes     (fn [{:keys [images volumes]}]
                             (cond-> ["All stopped containers"
                                      "Networks not used by a container"
                                      (if images "All images without a running container" "Dangling images")
                                      "Build cache"]
                               volumes (conj "Anonymous volumes not used by a container")))}})

;; no prune since startup answers 200 with an empty body, which arrives here as "" or {}
(defn- as-job
  [response]
  (when (and (map? response) (:status response))
    response))

(defn- job-handler
  []
  (ajax/get
    (routes/path-for-backend :maintenance-prune)
    {:state      [:loading?]
     :on-success (fn [{:keys [response origin?]}]
                   (when origin?
                     (state/update-value [:job] (as-job response) state/form-value-cursor)))}))

(defn- refresh-handler
  [_]
  (when (= "running" (get-in (state/get-value state/form-value-cursor) [:job :status]))
    (ajax/get
      (routes/path-for-backend :maintenance-prune)
      {:on-success (fn [{:keys [response]}]
                     (state/update-value [:job] (as-job response) state/form-value-cursor))})))

(defn- close-dialog!
  []
  (state/update-value [:dialog] nil state/form-state-cursor))

(defn- prune-handler
  [kind options]
  (ajax/post
    (routes/path-for-backend :maintenance-prune)
    {:params     (assoc options :kind kind)
     :state      [:processing?]
     :on-success (fn [{:keys [response]}]
                   (close-dialog!)
                   (state/update-value [:job] (as-job response) state/form-value-cursor))
     :on-error   (fn [{:keys [response]}]
                   (close-dialog!)
                   (message/error (str "Prune failed to start. " (:error response))))}))

(defn- init-form-state
  []
  (state/set-value {:loading?    true
                    :processing? false
                    :dialog      nil
                    :ack         false
                    :options     {"volumes" {} "images" {} "system" {}}} state/form-state-cursor))

(def mixin-init-form
  (mixin/init-form
    (fn [_]
      (init-form-state)
      (job-handler))))

(defn- destructive?
  [kind options]
  (->> (get-in prunes [kind :options])
       (some #(get options (:key %)))
       (boolean)))

(rum/defc option-checkbox < rum/static [kind options {:keys [key label warning]}]
  (let [checked? (boolean (get options key))]
    (html
      [:div.Swarmpit-maintenance-option {:key (name key)}
       (comp/form-control-label
         {:control (comp/checkbox
                     {:checked  checked?
                      :color    "primary"
                      :onChange #(state/update-value [:options kind key] (-> % .-target .-checked) state/form-state-cursor)})
          :label   label})
       (when checked?
         (comp/typography
           {:variant   "body2"
            :className "Swarmpit-maintenance-warning"} warning))])))

(rum/defc prune-card < rum/static [kind options running?]
  (let [{:keys [title description]} (get prunes kind)]
    (comp/card
      {:className "Swarmpit-form-card Swarmpit-fcard Swarmpit-maintenance-card"
       :key       kind}
      (comp/box
        {:className "Swarmpit-fcard-header"}
        (comp/typography
          {:className "Swarmpit-fcard-header-title"
           :variant   "h6"
           :component "div"} title))
      (comp/card-content
        {:className "Swarmpit-fcard-content"}
        (comp/typography
          {:variant   "body2"
           :className "Swarmpit-maintenance-description"} description)
        (map #(rum/with-key (option-checkbox kind options %) (name (:key %)))
             (get-in prunes [kind :options])))
      (comp/card-actions
        {:className "Swarmpit-fcard-actions"}
        (comp/button
          {:variant  "outlined"
           :color    "primary"
           :disabled running?
           :onClick  #(do (state/update-value [:ack] false state/form-state-cursor)
                          (state/update-value [:dialog] kind state/form-state-cursor))}
          (str title "…"))))))

(rum/defc confirm-dialog < rum/static [kind options ack processing?]
  (let [{:keys [title removes]} (get prunes kind)
        danger? (destructive? kind options)]
    (comp/dialog
      {:open      (some? kind)
       :onClose   close-dialog!
       :maxWidth  "sm"
       :fullWidth true}
      (when kind
        (comp/dialog-title {} (str title " on all nodes?")))
      (when kind
        (comp/dialog-content
          {}
          (html
            [:div
             [:p "This removes, on every ready and active node:"]
             [:ul (map-indexed (fn [i text] [:li {:key i} text]) (removes options))]
             [:p.Swarmpit-maintenance-note
              "Drained, paused and down nodes are skipped. Anything removed cannot be recovered."]
             (when danger?
               [:div.Swarmpit-maintenance-danger
                (->> (get-in prunes [kind :options])
                     (filter #(get options (:key %)))
                     (map (fn [{:keys [key warning]}] [:p {:key (name key)} warning])))
                (comp/form-control-label
                  {:control (comp/checkbox
                              {:checked  (boolean ack)
                               :color    "primary"
                               :onChange #(state/update-value [:ack] (-> % .-target .-checked) state/form-state-cursor)})
                   :label   "I understand this data will be deleted permanently"})])])))
      (comp/dialog-actions
        {}
        (comp/button
          {:onClick close-dialog!
           :color   "primary"} "Cancel")
        (comp/button
          {:onClick  #(prune-handler kind options)
           :color    "primary"
           :variant  "contained"
           :disabled (or processing? (and danger? (not ack)))}
          "Prune")))))

(defn- state-label
  [state]
  (case state
    "complete" "Done"
    "failed" "Failed"
    "rejected" "Rejected"
    state))

(rum/defc job-card < rum/static [{:keys [kind status startedAt finishedAt error nodes]}]
  (comp/card
    {:className "Swarmpit-form-card Swarmpit-fcard"}
    (comp/box
      {:className "Swarmpit-fcard-header"}
      (comp/typography
        {:className "Swarmpit-fcard-header-title"
         :variant   "h6"
         :component "div"}
        (str "Last run: " (get-in prunes [kind :title]))))
    (comp/card-content
      {:className "Swarmpit-fcard-content"}
      (html
        [:div
         [:p.Swarmpit-maintenance-note
          (case status
            "running" (str "Running since " (time/humanize startedAt) "…")
            "done" (str "Finished " (time/humanize finishedAt))
            "timeout" (str "Gave up waiting " (time/humanize finishedAt) "; some nodes did not report back")
            "failed" (str "Failed " (time/humanize finishedAt) ": " error)
            status)]
         (when (= "running" status)
           (comp/linear-progress {}))
         (when (seq nodes)
           (comp/table
             {:size "small"}
             (comp/table-head
               {}
               (comp/table-row
                 {}
                 (comp/table-cell {} "Node")
                 (comp/table-cell {} "Result")
                 (comp/table-cell {:align "right"} "Reclaimed")))
             (comp/table-body
               {}
               (map (fn [{:keys [node state reclaimed output] task-error :error}]
                      (comp/table-row
                        {:key node}
                        (comp/table-cell {} node)
                        (comp/table-cell
                          {}
                          (html
                            [:details.Swarmpit-maintenance-details
                             [:summary (state-label state) (when task-error (str ": " task-error))]
                             [:pre.Swarmpit-maintenance-output
                              (if (seq output) (clojure.string/join "\n" output) "No output")]]))
                        (comp/table-cell
                          {:align     "right"
                           :className "Swarmpit-maintenance-reclaimed"}
                          (or reclaimed "–"))))
                    nodes))))]))))

(rum/defc form < rum/reactive
                 mixin-init-form
                 (mixin/refresh-form refresh-handler)
  [_]
  (let [{:keys [job]} (state/react state/form-value-cursor)
        {:keys [loading? processing? dialog ack options]} (state/react state/form-state-cursor)
        running? (= "running" (:status job))]
    (progress/form
      loading?
      (comp/mui
        (html
          [:div.Swarmpit-form
           [:div.Swarmpit-form-context
            (comp/container
              {:maxWidth  "md"
               :className "Swarmpit-container"}
              (comp/grid
                {:container true
                 :spacing   2}
                (when job
                  (comp/grid {:item true :xs 12} (job-card job)))
                (map (fn [kind]
                       (comp/grid {:item true :xs 12 :md 4 :key kind}
                                  (prune-card kind (get options kind) running?)))
                     ["volumes" "images" "system"])))
            (confirm-dialog dialog (get options dialog) ack processing?)]])))))
