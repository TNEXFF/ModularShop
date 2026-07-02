import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// The React client is part of the SAME deployable unit as the API (a core Modular Monolith trait):
//   - In DEV: `pnpm dev` serves the SPA on :5173 and proxies /api to the ASP.NET host on :5080.
//   - In PROD: `pnpm build` emits the SPA straight into the API's wwwroot, so the API serves it.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080'
    }
  },
  build: {
    outDir: '../src/ModularShop.Server/wwwroot',
    emptyOutDir: true
  }
})
