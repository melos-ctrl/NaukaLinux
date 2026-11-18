<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'

const route = useRoute()
const token = typeof route.query.token === 'string' ? route.query.token : ''
const email = ref(typeof route.query.email === 'string' ? route.query.email : '')
const message = ref('')
const verified = ref(false)
const sending = ref(false)

const getResponseMessage = async (response: Response) => {
  const text = await response.text()
  try {
    return JSON.parse(text).message ?? text
  } catch {
    return text
  }
}

onMounted(async () => {
  if (!token) return

  message.value = 'Potwierdzam adres e-mail...'
  try {
    const response = await fetch('/api/auth/verify-email', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token })
    })
    message.value = await getResponseMessage(response)
    verified.value = response.ok
  } catch (error) {
    console.error(error)
    message.value = 'Błąd połączenia z serwerem.'
  }
})

const resendVerification = async () => {
  if (!email.value.trim()) {
    message.value = 'Wpisz adres e-mail użyty podczas rejestracji.'
    return
  }

  sending.value = true
  try {
    const response = await fetch('/api/auth/resend-verification', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: email.value })
    })
    message.value = await getResponseMessage(response)
    verified.value = false
  } catch (error) {
    console.error(error)
    message.value = 'Błąd połączenia z serwerem.'
  } finally {
    sending.value = false
  }
}
</script>

<template>
  <div class="flex min-h-screen flex-col items-center justify-center px-4">
    <div class="flex w-full max-w-md flex-col items-center rounded bg-carbon-black p-8 shadow">
      <h1 class="text-3xl font-bold text-floral-white">
        {{ token ? 'Potwierdzenie adresu' : 'Wyślij link ponownie' }}
      </h1>

      <p v-if="message" class="mt-4 text-center" :class="verified ? 'text-emerald-400' : 'text-floral-white'">
        {{ message }}
      </p>

      <form v-if="!verified" class="mt-5 flex w-full flex-col gap-3" @submit.prevent="resendVerification">
        <label for="email" class="text-floral-white">E-mail</label>
        <input
          id="email"
          v-model="email"
          class="rounded border border-silver bg-carbon-black px-4 py-2 text-floral-white focus:outline-none focus:ring-2 focus:ring-spicy-paprika"
          type="email"
          autocomplete="email"
          required
        >
        <button
          type="submit"
          :disabled="sending"
          class="mt-2 rounded bg-spicy-paprika px-4 py-2 text-floral-white transition hover:opacity-90 disabled:opacity-60"
        >
          {{ sending ? 'Wysyłanie...' : 'Wyślij link weryfikacyjny' }}
        </button>
      </form>

      <router-link to="/login" class="mt-5 text-spicy-paprika hover:underline">
        Przejdź do logowania
      </router-link>
    </div>
  </div>
</template>