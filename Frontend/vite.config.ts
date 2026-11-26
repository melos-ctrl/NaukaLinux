import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueJsx from '@vitejs/plugin-vue-jsx'
import vueDevTools from 'vite-plugin-vue-devtools'
import tailwindcss from '@tailwindcss/vite'

// Pulpity kursantów: /desktop/3101/ -> 127.0.0.1:3101, /desktop/3102/ -> 127.0.0.1:3102 itd.
// (na produkcji robi to nginx). 20 portów z zapasem - backend przydziela 3101..3100+MaxContainers.
const desktopProxies = Object.fromEntries(
  Array.from({ length: 20 }, (_, i) => 3101 + i).map((port) => [
    `/desktop/${port}/`,
    {
      target: `http://127.0.0.1:${port}`,
      changeOrigin: true,
      ws: true, // KasmVNC przesyła obraz przez WebSocket
    },
  ]),
)

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
      '/avatars': {
        target: 'http://localhost:5042',
        changeOrigin: true,
        secure: false,
      },
      ...desktopProxies,
    }
  }
})
