import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// Backend dev host (ASP.NET Core). Both launch profiles bind the http endpoint
// (the `https` profile adds 7206 on top), so proxying over http works regardless
// of which profile `dotnet run` picked and needs no self-signed-cert handling.
// See src/Jadlify.API/Properties/launchSettings.json.
const API_TARGET = 'http://localhost:5182'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Forward API + health calls to the running backend so local dev mirrors the
    // single-origin production model (no CORS).
    proxy: {
      '/api': {
        target: API_TARGET,
        changeOrigin: true,
      },
      '/health': {
        target: API_TARGET,
        changeOrigin: true,
      },
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./vitest.setup.ts'],
    exclude: ['**/node_modules/**', '**/dist/**', 'tests/e2e/**'],
    css: true,
    // Placeholder public Supabase config so modules that import the client (which
    // throws on missing env) are safe to load under test. No network is made.
    env: {
      VITE_SUPABASE_URL: 'http://localhost:54321',
      VITE_SUPABASE_ANON_KEY: 'test-anon-key',
    },
  },
})
