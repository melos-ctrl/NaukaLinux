<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Turnstile } from '@sctg/turnstile-vue3'
import AuthCard from '@/components/AuthCard.vue'


const username = ref('')
const password = ref('')
const message = ref('')

// Token do przechowywania tokenu Turnstile, używany do weryfikacji logowania.
const turnstileToken = ref('')
const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY as string
// Token Turnstile jest jednorazowy - po każdej próbie zmieniamy klucz,
// co tworzy widget od nowa i generuje świeży token.
const turnstileKey = ref(0)

const resetTurnstile = () => {
  turnstileToken.value = ''
  turnstileKey.value++
}

const router = useRouter()

const handleLogin = async () => {
  if (!turnstileToken.value) {
    message.value = 'Potwierdź, że nie jesteś robotem.'
    return
  }

  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        username: username.value,
        password: password.value,
        cfToken: turnstileToken.value // Token Turnstile sprawdzany przez backend w Cloudflare
      })
    })

    if (response.ok) {
      const data = await response.json()

      localStorage.setItem('token', data.token)

      if (data.role) {
        localStorage.setItem('role', data.role)
      } else {
        localStorage.setItem('role', 'student')
      }

      router.push('/')
    } else {
      const errorText = await response.text()
      let errorData: { message?: string } = {}
      try {
        errorData = JSON.parse(errorText) as { message?: string }
      } catch {
      }
      message.value = errorData.message ?? (errorText || 'Nieprawidłowy login lub hasło.')
      resetTurnstile() // zużyty token - potrzebny nowy do kolejnej próby
    }
  } catch (error) {
    console.error(error)
    message.value = 'Błąd połączenia z serwerem.'
    resetTurnstile()
  }
}
</script>

<template>
  <AuthCard eyebrow="Witaj ponownie" title="Logowanie" lead="Zaloguj się, aby wrócić do swoich kursów.">
    <form @submit.prevent="handleLogin" class="space-y-4">
      <div>
        <label for="username" class="label">Nazwa użytkownika</label>
        <input v-model="username" class="input" type="text" id="username" autocomplete="username" required>
      </div>

      <div>
        <div class="flex items-baseline justify-between">
          <label for="password" class="label">Hasło</label>
          <router-link to="/forgot-password" class="text-sm font-medium text-paprika-600 hover:underline">Nie pamiętasz hasła?</router-link>
        </div>
        <input v-model="password" class="input" type="password" id="password" autocomplete="current-password" required>
      </div>

      <!-- Widget Turnstile -->
      <div class="flex justify-center">
        <Turnstile
          :key="turnstileKey"
          :siteKey="turnstileSiteKey"
          v-model="turnstileToken"
        />
      </div>

      <p v-if="message" class="alert alert-error" role="alert">{{ message }}</p>

      <button type="submit" class="btn btn-primary w-full">Zaloguj się</button>
    </form>

    <template #footer>
      Nie masz konta?
      <router-link to="/register" class="font-semibold text-paprika-600 hover:underline">Zarejestruj się</router-link>
    </template>
  </AuthCard>
</template>
