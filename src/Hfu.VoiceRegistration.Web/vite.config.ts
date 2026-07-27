/// <reference types="vitest" />
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true
  },
  server: {
    port: 5173,
    proxy: {
      "/health": "http://localhost:5076",
      "/api": {
        target: "http://localhost:5076",
        ws: true
      },
      "/hubs": {
        target: "http://localhost:5076",
        ws: true
      }
    }
  }
});
