import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api/warehouse': {
        target: process.env.VITE_WAREHOUSE_API_URL || 'http://localhost:5280',
        changeOrigin: true,
        secure: false,
      },
      '/api/catalog': {
        target: process.env.VITE_CATALOG_API_URL || 'http://localhost:5301',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
