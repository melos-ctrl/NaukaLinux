<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'

const desktopUrl = ref<string | null>(null)
const activeContainerId = ref<string | null>(null)
const isLoading = ref(false)

let heartbeatInterval: any = null

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

const startHeartbeat = (id: string) => {
  stopHeartbeat()
  heartbeatInterval = setInterval(() => {
    fetch(`http://localhost:5042/api/virtualdesktop/heartbeat?ContainerId=${id}`, { method: 'POST' })
      .catch(error => console.error(error))
  }, 30000)
}

const stopHeartbeat = () => {
  if (heartbeatInterval) {
    clearInterval(heartbeatInterval)
    heartbeatInterval = null
  }
}

onMounted(() => {
  const savedUrl = localStorage.getItem('desktopUrl')
  const savedContainerId = localStorage.getItem('activeContainerId')

  if (savedUrl && savedContainerId) {
    desktopUrl.value = savedUrl
    activeContainerId.value = savedContainerId
    startHeartbeat(savedContainerId)
  }
})

onUnmounted(() => {
  stopHeartbeat()
})

const startSession = async () => {
  if (activeContainerId.value) return

  isLoading.value = true
  try {
    const response = await fetch('http://localhost:5042/api/virtualdesktop/start', { method: 'POST' })

    if (response.ok) {
      const data = await response.json()
      activeContainerId.value = data.containerId
      localStorage.setItem('activeContainerId', data.containerId)

      startHeartbeat(data.containerId)

      await delay(8000)

      desktopUrl.value = data.connectionUrl
      localStorage.setItem('desktopUrl', data.connectionUrl)
    }
  } catch (error) {
    console.error(error)
  } finally {
    isLoading.value = false
  }
}

const stopSession = async () => {
  if (!activeContainerId.value) return

  try {
    await fetch(`http://localhost:5042/api/virtualdesktop/stop?ContainerId=${activeContainerId.value}`, { method: 'POST' })
    
    stopHeartbeat()
    
    desktopUrl.value = null
    activeContainerId.value = null

    localStorage.removeItem('activeContainerId')
    localStorage.removeItem('desktopUrl')
  } catch (error) {
    console.error(error)
  }
}
</script>

<template>
  <div class="flex flex-col items-center gap-4 p-5 w-full">
    <h2 class="text-2xl font-bold text-charcoal-brown">Wirtualny Linux MATE (KasmVNC)</h2>

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

    <div v-if="desktopUrl" class="w-full h-[750px] border-4 border-slate-800 rounded-lg overflow-hidden shadow-2xl relative mt-4">
      <iframe
        :src="desktopUrl"
        class="w-full h-full border-none bg-black"
        allow="clipboard-read; clipboard-write"
      ></iframe>
    </div>
  </div>
</template>
