<script setup lang="ts">
import { ref } from 'vue'
import { Turnstile } from '@sctg/turnstile-vue3'

const email = ref('')
const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const message = ref('')
const registrationComplete = ref(false)
// Token do przechowywania tokenu Turnstile, używany do weryfikacji rejestracji.
const turnstileToken = ref('')
const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY as string

const handleRegister = async () => {
  if (password.value !== confirmPassword.value) {
    registrationComplete.value = false
    message.value = 'Hasła nie są identyczne!'
    return
  }

  if (!turnstileToken.value) {
    registrationComplete.value = false
    message.value = 'Proszę potwierdzić, że nie jesteś robotem.'
    return
  }


  try {
    const response = await fetch('/api/auth/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        email: email.value,
        username: username.value,
        password: password.value,
        cfToken: turnstileToken.value // Dodanie tokenu Turnstile do żądania rejestracji
      })
    })

    if (response.ok) {
      registrationComplete.value = true
      message.value = 'Konto utworzone. Sprawdź skrzynkę i potwierdź adres e-mail.'
    } else {
      const errorText = await response.text()
      let errorData: { message?: string; accountCreated?: boolean } = {}
      try {
        errorData = JSON.parse(errorText) as { message?: string; accountCreated?: boolean }
      } catch {
        // Plain-text responses are still shown as-is.
      }
      registrationComplete.value = errorData.accountCreated === true
      message.value = errorData.message ?? `Błąd: ${errorText}`
      turnstileToken.value = '' // resetowanie Turnsite po nieudanej rejestracji.
    }
  } catch (error) {
    console.error(error)
    registrationComplete.value = false
    message.value = 'Błąd połączenia z serwerem.'
  }
}
</script>

<template>
  <div class="flex flex-col items-center justify-center h-screen">
    <div class="bg-carbon-black p-8 rounded shadow flex flex-col items-center">
      <h1 class="text-3xl font-bold text-floral-white">Rejestracja</h1>

      <form @submit.prevent="handleRegister" class="flex flex-col w-full max-w-sm mt-4 gap-2">

        <label for="email" class="text-floral-white">E-mail</label>
        <input v-model="email" class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="email" id="email" required>

        <label for="username" class="text-floral-white">Nazwa użytkownika</label>
        <input v-model="username" class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="text" id="username" required>

        <label for="password" class="text-floral-white">Hasło</label>
        <input v-model="password" class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="password" id="password" required>

        <label for="confirmPassword" class="text-floral-white">Powtórz Hasło</label>
        <input v-model="confirmPassword" class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="password" id="confirmPassword" required>

        <!-- Widget Turnstile -->
        <div class="mt-4 flex justify-center">
          <Turnstile 
            :siteKey="turnstileSiteKey" 
            v-model="turnstileToken"
          />
        </div>


        <button type="submit" class="bg-spicy-paprika text-floral-white px-4 py-2 rounded mt-4 hover:bg-floral-white hover:text-charcoal-brown  transition">Zarejestruj się</button>
      </form>

      <p v-if="message" class="mt-4 text-center font-bold" :class="registrationComplete ? 'text-emerald-400' : 'text-red-500'">{{ message }}</p>
      <router-link
        v-if="registrationComplete"
        :to="{ name: 'resend-verification', query: { email } }"
        class="mt-2 text-sm text-spicy-paprika hover:underline"
      >Nie dostałeś maila? Wyślij link ponownie</router-link>

      <span class="mt-4 text-sm text-floral-white">Masz już konto? <a href="/login" class="text-spicy-paprika hover:underline">Zaloguj się</a></span>
    </div>
  </div>
</template>
