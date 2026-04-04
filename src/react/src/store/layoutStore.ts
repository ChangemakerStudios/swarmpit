import { create } from "zustand";

type ThemeMode = "light" | "dark";

interface LayoutState {
  sidebarOpen: boolean;
  themeMode: ThemeMode;
  toggleSidebar: () => void;
  setSidebarOpen: (open: boolean) => void;
  toggleTheme: () => void;
}

export const useLayoutStore = create<LayoutState>((set) => ({
  sidebarOpen: false,
  themeMode: (localStorage.getItem("themeMode") as ThemeMode) ?? "light",
  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  setSidebarOpen: (open) => set({ sidebarOpen: open }),
  toggleTheme: () =>
    set((s) => {
      const next = s.themeMode === "light" ? "dark" : "light";
      localStorage.setItem("themeMode", next);
      return { themeMode: next };
    }),
}));
