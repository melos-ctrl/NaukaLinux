import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueJsx(),
    vueDevTools(),
    tailwindcss(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  // dodanie konfiguracji serwera deweloperskiego z włączonym odpytywaniem i pozbycie sie problemu development -> deployment
  server: {
    watch: {
      usePolling: true, // odpytywanie
      interval: 1000, // sprawdzenie zmian co 1000ms
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5042', // adres backendu
        changeOrigin: true,
        secure: false,
      },
    }
  }
})

// dodanie proxy pozwala mi pisanie w kodzie frontendu np. fetch('/api/endpoint') zamiast fetch('http://localhost:5042/api/endpoint') co pozwala na łatwiejsze przełączanie się między środowiskiem deweloperskim a produkcyjnym.
