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
  <div class="flex w-full flex-col items-center gap-4 p-5">
    <h2 class="section-title font-mono">Wirtualny Linux XFCE (KasmVNC)</h2>

    <div class="flex gap-4">
      <button
        v-if="!desktopUrl"
        type="button"
        @click="startSession"
        :disabled="isLoading"
        class="btn btn-primary"
      >
        {{ isLoading ? 'Klonowanie systemu...' : 'Uruchom środowisko' }}
      </button>

      <button
        v-if="desktopUrl"
        type="button"
        @click="stopSession"
        class="btn btn-danger"
      >
        Zakończ sesję i usuń dane
      </button>
    </div>

    <p v-if="errorMessage" role="alert" class="alert alert-error">{{ errorMessage }}</p>

    <div v-if="desktopUrl" class="relative mt-4 h-[750px] w-full overflow-hidden rounded-2xl border-4 border-carbon-black shadow-2xl">
      <iframe
        :src="desktopUrl"
        class="h-full w-full border-none bg-black"
        allow="clipboard-read; clipboard-write"
      ></iframe>
    </div>
  </div>
</template>
