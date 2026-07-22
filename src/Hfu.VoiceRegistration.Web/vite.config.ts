import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/health": "http://localhost:5076",
      "/api": "http://localhost:5076",
      "/hubs": {
        target: "http://localhost:5076",
        ws: true
      }
    }
  }
});
