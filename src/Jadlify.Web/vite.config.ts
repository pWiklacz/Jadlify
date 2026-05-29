import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// Backend dev host (ASP.NET Core). See src/Jadlify.API/Properties/launchSettings.json.
const API_TARGET = 'https://localhost:7206'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Forward API + health calls to the running backend so local dev mirrors the
    // single-origin production model (no CORS). secure:false accepts the dev
    // self-signed certificate.
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: true,
        secure: false,
      },
      '/health': {
        target: API_TARGET,
        changeOrigin: true,
        secure: false,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./vitest.setup.ts'],
    css: true,
  },
})
