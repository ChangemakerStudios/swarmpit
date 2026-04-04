import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 8501,
    proxy: {
      "/api": "http://localhost:8500",
      "/login": "http://localhost:8500",
      "/initialize": "http://localhost:8500",
      "/version": "http://localhost:8500",
      "/events": "http://localhost:8500",
    },
  },
});
