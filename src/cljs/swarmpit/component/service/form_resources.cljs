(ns swarmpit.component.service.form-resources
  (:require [material.components :as comp]
            [material.component.form :as form]
            [swarmpit.component.parser :refer [parse-int parse-float]]
            [swarmpit.component.state :as state]
            [sablono.core :refer-macros [html]]
            [clojure.walk :refer [keywordize-keys]]
            [rum.core :as rum]))

(enable-console-print!)

(def form-value-cursor (conj state/form-value-cursor :resources))

(def form-state-cursor (conj state/form-state-cursor :resources))

(defn- cpu-value
  [value]
  (if (or (nil? value) (zero? value))
    "unlimited"
    value))

(defn- set-memory-validity
  "Track browser-invalid number inputs (e.g. '1gb' in Firefox reports an empty
   value with validity.badInput) so bad input surfaces instead of being
   silently dropped from the payload."
  [field bad-input?]
  (state/update-value [:validity field] bad-input? form-state-cursor)
  (state/update-value [:valid?]
                      (->> (state/get-value form-state-cursor)
                           :validity
                           (vals)
                           (not-any? true?))
                      form-state-cursor))

(defn- form-cpu-reservation [value]
  (html
    [:div.Swarmpit-margin-normal
     [:div.Swarmpit-service-slider-title
      (str "CPU  " "(" (cpu-value value) ")")]
     [:div
      (comp/slider
        {:key          "cpu-reservation"
         :min          0
         :max          4
         :step         0.10
         :defaultValue 0
         :marks        true
         :value        value
         :style        {:maxWidth "300px"}
         :onChange     (fn [e v] (state/update-value [:reservation :cpu] (parse-float v) form-value-cursor))})]]))

(defn- form-memory-reservation [value error?]
  (comp/text-field
    {:label           "Memory"
     :key             "memory-reservation"
     :type            "number"
     :variant         "outlined"
     :margin          "normal"
     :style           {:maxWidth "300px"}
     :error           error?
     :helperText      (if error?
                        "Invalid value. Enter memory in MiB (e.g. 1024)"
                        "Use minimum of 4 MiB or leave blank for unlimited")
     :min             4
     :fullWidth       true
     :defaultValue    value
     :InputLabelProps {:shrink true}
     :onChange        #(let [target (.-target %)]
                         (set-memory-validity :reservation-memory (-> target .-validity .-badInput))
                         (state/update-value [:reservation :memory] (parse-int (.-value target)) form-value-cursor))}))

(defn- form-cpu-limit [value]
  (html
    [:div.Swarmpit-margin-normal
     [:div.Swarmpit-service-slider-title
      (str "CPU  " "(" (cpu-value value) ")")]
     [:div
      (comp/slider
        {:key          "cpu-limit"
         :min          0
         :max          4
         :step         0.10
         :defaultValue 0
         :value        value
         :marks        true
         :style        {:maxWidth "300px"}
         :onChange     (fn [e v] (state/update-value [:limit :cpu] (parse-float v) form-value-cursor))})]]))

(defn- form-memory-limit [value error?]
  (comp/text-field
    {:label           "Memory"
     :key             "memory-limit"
     :type            "number"
     :variant         "outlined"
     :margin          "normal"
     :style           {:maxWidth "300px"}
     :error           error?
     :helperText      (if error?
                        "Invalid value. Enter memory in MiB (e.g. 1024)"
                        "Use minimum of 4 MiB or leave blank for unlimited")
     :min             4
     :fullWidth       true
     :defaultValue    value
     :InputLabelProps {:shrink true}
     :onChange        #(let [target (.-target %)]
                         (set-memory-validity :limit-memory (-> target .-validity .-badInput))
                         (state/update-value [:limit :memory] (parse-int (.-value target)) form-value-cursor))}))

(rum/defc form < rum/reactive []
  (let [{:keys [reservation limit]} (state/react form-value-cursor)
        {:keys [validity]} (state/react form-state-cursor)]
    (comp/grid
      {:container true
       :spacing   5}
      (comp/grid
        {:item true
         :xs   12
         :sm   6}
        (form/subsection "Reservation")
        (form-memory-reservation (:memory reservation) (true? (:reservation-memory validity)))
        (form-cpu-reservation (:cpu reservation)))
      (comp/grid
        {:item true
         :xs   12
         :sm   6}
        (form/subsection "Limit")
        (form-memory-limit (:memory limit) (true? (:limit-memory validity)))
        (form-cpu-limit (:cpu limit))))))
