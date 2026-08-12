(ns material.component.pagination
  "Paging shared by every list. The chosen page size is remembered across lists
   and across reloads - picking 100 on services and then opening tasks should
   not silently put you back on 30."
  (:require [material.components :as cmp]
            [swarmpit.storage :as storage]
            [rum.core :as rum]))

(def page-sizes [10 25 50 100])

(def ^:private default-page-size 25)

(def ^:private storage-key "pageSize")

(defn- stored-page-size
  []
  (let [size (js/parseInt (or (storage/get storage-key) "") 10)]
    (if (some #(= size %) page-sizes)
      size
      default-page-size)))

(defonce state
  (atom {:rowsPerPage (stored-page-size)
         :page        0}))

(defn reset-page!
  "Back to the first page. Lists call this when they mount, so arriving at a
   list never lands you on a page that its items may not reach."
  []
  (swap! state assoc :page 0))

(defn page-items
  "The slice of `items` belonging to the current page.

   Clamped deliberately: filtering a long list while on a later page leaves the
   offset beyond the end, and an unclamped subvec throws rather than showing an
   empty page."
  [items {:keys [page rowsPerPage]}]
  (let [items (into [] items)
        size  (count items)
        from  (min (* page rowsPerPage) size)
        to    (min (+ from rowsPerPage) size)]
    (subvec items from to)))

(defn- change-page-size!
  [event]
  (let [size (js/parseInt (-> event .-target .-value) 10)]
    (storage/add storage-key (str size))
    (swap! state assoc :rowsPerPage size :page 0)))

(rum/defc pagination < rum/reactive
  "Page controls for `items`. Shown once there are more items than the smallest
   page size, so the size selector stays reachable even when the current page
   happens to hold everything."
  [items]
  (let [{:keys [rowsPerPage page]} (rum/react state)]
    (when (> (count items) (apply min page-sizes))
      (cmp/table-pagination
        {:rowsPerPageOptions  page-sizes
         :component           "div"
         :count               (count items)
         :rowsPerPage         rowsPerPage
         :page                page
         :labelRowsPerPage    "Rows per page"
         :onChangePage        (fn [_ new-page] (swap! state assoc :page new-page))
         :onChangeRowsPerPage change-page-size!}))))
