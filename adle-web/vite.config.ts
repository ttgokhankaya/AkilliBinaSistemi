/// <reference types="vitest/config" />
import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  // Production build lands in the API's wwwroot so `dotnet run` serves the SPA
  // and the API from a single origin (no proxy needed in prod).
  build: {
    outDir: '../Adle.Api/wwwroot',
    emptyOutDir: true,
  },
  server: {
    // Dev only: proxy API calls to the ASP.NET Core backend (http launch profile).
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
    css: false,
  },
})
