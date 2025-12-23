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
    port: 5174,
    proxy: {
      '/api/shipping': {
        target: process.env.VITE_SHIPPING_API_URL || 'http://localhost:5281',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
