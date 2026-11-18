<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'

interface DesktopSessionDto {
  port: number
  containerId: string
  connectionUrl: string
}

const desktopUrl = ref<string | null>(null)
const activeContainerId = ref<string | null>(null)
const isLoading = ref(false)
const errorMessage = ref('')

let heartbeatInterval: ReturnType<typeof setInterval> | null = null

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

const authHeaders = (): HeadersInit => {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

const resetSession = () => {
  stopHeartbeat()
  desktopUrl.value = null
  activeContainerId.value = null
}

const startHeartbeat = () => {
  stopHeartbeat()
  heartbeatInterval = setInterval(async () => {
    try {
      const response = await fetch('/api/virtualdesktop/heartbeat', { method: 'POST', headers: authHeaders() })
      // 404 = backend usunął kontener (np. po restarcie) - nie ma czego podtrzymywać
      if (response.status === 404 || response.status === 401) {
        resetSession()
        errorMessage.value = 'Sesja środowiska wygasła. Uruchom je ponownie.'
      }
    } catch (error) {
      console.error(error)
    }
  }, 30000)
}

const stopHeartbeat = () => {
  if (heartbeatInterval) {
    clearInterval(heartbeatInterval)
    heartbeatInterval = null
  }
}

// Backend pamięta, czy użytkownik ma już uruchomiony pulpit - nie trzymamy tego w localStorage
onMounted(async () => {
  if (!localStorage.getItem('token')) return

  try {
    const response = await fetch('/api/virtualdesktop/current', { headers: authHeaders() })
    if (response.status === 200) {
      const data: DesktopSessionDto = await response.json()
      activeContainerId.value = data.containerId
      desktopUrl.value = data.connectionUrl
      startHeartbeat()
    }
  } catch (error) {
    console.error(error)
  }
})

onUnmounted(() => {
  stopHeartbeat()
})

const startSession = async () => {
  if (activeContainerId.value) return

  errorMessage.value = ''

  if (!localStorage.getItem('token')) {
    errorMessage.value = 'Zaloguj się, aby uruchomić środowisko.'
    return
  }

  isLoading.value = true
  try {
    const response = await fetch('/api/virtualdesktop/start', { method: 'POST', headers: authHeaders() })

    if (!response.ok) {
      errorMessage.value = response.status === 401
        ? 'Sesja logowania wygasła. Zaloguj się ponownie.'
        : (await response.text()) || 'Nie udało się uruchomić środowiska.'
      return
    }

    const data: DesktopSessionDto = await response.json()
    activeContainerId.value = data.containerId
    startHeartbeat()

    // czekamy, aż KasmVNC w kontenerze wystartuje
    await delay(8000)

    desktopUrl.value = data.connectionUrl
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Nie udało się połączyć z serwerem.'
  } finally {
    isLoading.value = false
  }
}

const stopSession = async () => {
  if (!activeContainerId.value) return

  try {
    await fetch('/api/virtualdesktop/stop', { method: 'POST', headers: authHeaders() })
    resetSession()
  } catch (error) {
    console.error(error)
    errorMessage.value = 'Nie udało się zatrzymać środowiska.'
  }
}
</script>

<template>
  <div class="flex flex-col items-center gap-4 p-5 w-full">
    <h2 class="text-2xl font-bold text-charcoal-brown">Wirtualny Linux XFCE (KasmVNC)</h2>

    <div class="flex gap-4">
      <button
        v-if="!desktopUrl"
        @click="startSession"
        :disabled="isLoading"
        class="px-6 py-2 bg-spicy-paprika text-white rounded-md hover:bg-spicy-paprika/90 disabled:bg-charcoal-brown/40 font-bold shadow-md transition-colors"
      >
        {{ isLoading ? 'Klonowanie systemu...' : 'Uruchom środowisko' }}
      </button>

      <button
        v-if="desktopUrl"
        @click="stopSession"
        class="px-6 py-2 bg-red-600 text-white rounded-md hover:bg-red-700 font-bold shadow-md transition-colors"
      >
        Zakończ sesję i usuń dane
      </button>
    </div>

    <p v-if="errorMessage" role="alert" class="text-sm font-semibold text-red-700">{{ errorMessage }}</p>

    <div v-if="desktopUrl" class="w-full h-[750px] border-4 border-slate-800 rounded-lg overflow-hidden shadow-2xl relative mt-4">
      <iframe
        :src="desktopUrl"
        class="w-full h-full border-none bg-black"
        allow="clipboard-read; clipboard-write"
      ></iframe>
    </div>
  </div>
</template>