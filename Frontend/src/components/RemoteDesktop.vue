<script setup lang="ts">
import { computed, ref, onMounted, onUnmounted } from 'vue'

interface DesktopSessionDto {
  port: number
  containerId: string
  connectionUrl: string
  expiresAt: string
}

const desktopUrl = ref<string | null>(null)
const activeContainerId = ref<string | null>(null)
const expiresAt = ref<number | null>(null)
const now = ref(Date.now())
const isLoading = ref(false)
const errorMessage = ref('')
const isBusy = ref(false)

let heartbeatInterval: ReturnType<typeof setInterval> | null = null
let clockInterval: ReturnType<typeof setInterval> | null = null

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

const authHeaders = (): HeadersInit => {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

// Odczyt komunikatu z backendu: JSON { message } albo zwykły tekst
const readMessage = async (response: Response, fallback: string) => {
  const text = await response.text()
  try {
    return (JSON.parse(text) as { message?: string }).message || fallback
  } catch {
    return text || fallback
  }
}

// --- licznik czasu sesji ---
const secondsLeft = computed(() =>
  expiresAt.value === null ? null : Math.max(0, Math.floor((expiresAt.value - now.value) / 1000))
)
const timeLeftLabel = computed(() => {
  if (secondsLeft.value === null) return ''
  const minutes = Math.floor(secondsLeft.value / 60)
  const seconds = secondsLeft.value % 60
  return `${minutes}:${seconds.toString().padStart(2, '0')}`
})
const isEndingSoon = computed(() => secondsLeft.value !== null && secondsLeft.value <= 5 * 60)

const startClock = () => {
  stopClock()
  clockInterval = setInterval(() => {
    now.value = Date.now()
    if (secondsLeft.value === 0) {
      // Backend i tak usunie kontener - nie czekamy na kolejny heartbeat
      resetSession()
      errorMessage.value = 'Minęła godzina – sesja środowiska została zakończona. Możesz uruchomić je ponownie.'
    }
  }, 1000)
}

const stopClock = () => {
  if (clockInterval) {
    clearInterval(clockInterval)
    clockInterval = null
  }
}

const applySession = (data: DesktopSessionDto) => {
  activeContainerId.value = data.containerId
  expiresAt.value = Date.parse(data.expiresAt)
  now.value = Date.now()
  startHeartbeat()
  startClock()
}

const resetSession = () => {
  stopHeartbeat()
  stopClock()
  desktopUrl.value = null
  activeContainerId.value = null
  expiresAt.value = null
}

const startHeartbeat = () => {
  stopHeartbeat()
  heartbeatInterval = setInterval(async () => {
    try {
      const response = await fetch('/api/virtualdesktop/heartbeat', { method: 'POST', headers: authHeaders() })
      // 410 = minął limit czasu sesji
      if (response.status === 410) {
        errorMessage.value = await readMessage(response, 'Minął limit czasu sesji.')
        resetSession()
      }
      // 404 = backend usunął kontener (np. po restarcie) - nie ma czego podtrzymywać
      else if (response.status === 404 || response.status === 401) {
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
      desktopUrl.value = data.connectionUrl
      applySession(data)
    }
  } catch (error) {
    console.error(error)
  }
})

onUnmounted(() => {
  stopHeartbeat()
  stopClock()
})

const startSession = async () => {
  if (activeContainerId.value) return

  errorMessage.value = ''
  isBusy.value = false

  if (!localStorage.getItem('token')) {
    errorMessage.value = 'Zaloguj się, aby uruchomić środowisko.'
    return
  }

  isLoading.value = true
  try {
    const response = await fetch('/api/virtualdesktop/start', { method: 'POST', headers: authHeaders() })

    if (!response.ok) {
      isBusy.value = response.status === 503
      errorMessage.value = response.status === 401
        ? 'Sesja logowania wygasła. Zaloguj się ponownie.'
        : await readMessage(response, 'Nie udało się uruchomić środowiska.')
      return
    }

    const data: DesktopSessionDto = await response.json()
    applySession(data)

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

    <div class="flex flex-wrap items-center justify-center gap-4">
      <button
        v-if="!desktopUrl"
        type="button"
        @click="startSession"
        :disabled="isLoading"
        class="btn btn-primary"
      >
        {{ isLoading ? 'Klonowanie systemu...' : 'Uruchom środowisko' }}
      </button>

      <template v-if="desktopUrl">
        <span
          class="badge font-mono text-sm"
          :class="isEndingSoon ? 'badge-danger' : 'badge-info'"
          role="timer"
          :aria-label="`Pozostały czas sesji: ${timeLeftLabel}`"
        >
          Pozostało {{ timeLeftLabel }}
        </span>
        <button type="button" @click="stopSession" class="btn btn-danger">
          Zakończ sesję i usuń dane
        </button>
      </template>
    </div>

    <p v-if="desktopUrl && isEndingSoon" class="alert alert-error" role="status">
      Sesja kończy się za mniej niż 5 minut. Zapisz to, czego potrzebujesz – po zakończeniu dane z maszyny znikną.
    </p>

    <p
      v-if="errorMessage"
      :role="isBusy ? 'status' : 'alert'"
      class="alert max-w-xl text-center"
      :class="isBusy ? 'alert-info' : 'alert-error'"
    >{{ errorMessage }}</p>

    <div v-if="desktopUrl" class="relative mt-4 h-[750px] w-full overflow-hidden rounded-2xl border-4 border-carbon-black shadow-2xl">
      <iframe
        :src="desktopUrl"
        class="h-full w-full border-none bg-black"
        allow="clipboard-read; clipboard-write"
      ></iframe>
    </div>
  </div>
</template>