import { create } from "zustand";

interface AuthState {
  token: string | null;
  username: string | null;
  role: string | null;
  isAuthenticated: boolean;
  setAuth: (token: string, username: string, role: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => {
  const token = localStorage.getItem("token");
  const username = localStorage.getItem("username");
  const role = localStorage.getItem("role");

  return {
    token,
    username,
    role,
    isAuthenticated: !!token,
    setAuth: (token, username, role) => {
      localStorage.setItem("token", token);
      localStorage.setItem("username", username);
      localStorage.setItem("role", role);
      set({ token, username, role, isAuthenticated: true });
    },
    logout: () => {
      localStorage.removeItem("token");
      localStorage.removeItem("username");
      localStorage.removeItem("role");
      set({ token: null, username: null, role: null, isAuthenticated: false });
    },
  };
});
