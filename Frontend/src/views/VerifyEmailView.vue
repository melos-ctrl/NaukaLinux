<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import AuthCard from '@/components/AuthCard.vue'

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
  <AuthCard
    eyebrow="Adres e-mail"
    :title="token ? 'Potwierdzenie adresu' : 'Wyślij link ponownie'"
  >
    <p
      v-if="message"
      class="alert"
      :class="verified ? 'alert-success' : 'alert-info'"
      role="status"
    >
      {{ message }}
    </p>

    <form v-if="!verified" class="space-y-4" @submit.prevent="resendVerification">
      <div>
        <label for="email" class="label">E-mail</label>
        <input
          id="email"
          v-model="email"
          class="input"
          type="email"
          autocomplete="email"
          required
        >
      </div>
      <button type="submit" :disabled="sending" class="btn btn-primary w-full">
        {{ sending ? 'Wysyłanie...' : 'Wyślij link weryfikacyjny' }}
      </button>
    </form>

    <template #footer>
      <router-link to="/login" class="font-semibold text-paprika-600 hover:underline">Przejdź do logowania</router-link>
    </template>
  </AuthCard>
</template>
