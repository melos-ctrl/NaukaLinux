<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { Turnstile } from '@sctg/turnstile-vue3'


const username = ref('')
const password = ref('')
const message = ref('')

// Token do przechowywania tokenu Turnstile, używany do weryfikacji logowania.
const turnstileToken = ref('')
const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY as string


const router = useRouter()

const handleLogin = async () => {
  try {
    const response = await fetch('/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        username: username.value,
        password: password.value,
        cfToken: turnstileToken.value // Dodanie tokenu Turnstile do żądania logowania

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
      message.value = 'Nieprawidłowy login lub hasło.'
      turnstileToken.value = '' // resetowanie Turnsite po nieudanej próbie logowania
    }
  } catch (error) {
    console.error(error)
    message.value = 'Błąd połączenia z serwerem.'
  }
}
</script>

<template>
  <div class="flex flex-col items-center justify-center h-screen">
    <div class="bg-carbon-black p-8 rounded shadow flex flex-col items-center">
      <h1 class="text-3xl font-bold text-floral-white">Logowanie</h1>

      <form @submit.prevent="handleLogin" class="flex flex-col w-full max-w-sm mt-4 gap-2">
        <label for="username" class="text-floral-white">Nazwa użytkownika</label>
        <input v-model="username" class=" bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="text" id="username" required>

        <label for="password" class="text-floral-white">Hasło</label>
        <input v-model="password" class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" type="password" id="password" required>

        <router-link to="/forgot-password" class="text-sm text-spicy-paprika hover:underline mt-2">Nie pamiętasz hasła?</router-link>

        <!-- Widget Turnstile -->
        <div class="mt-4 flex justify-center">
          <Turnstile 
            :siteKey="turnstileSiteKey" 
            v-model="turnstileToken"
          />
        </div>

        <button type="submit" class="bg-spicy-paprika text-floral-white px-4 py-2 rounded mt-4 hover:bg-floral-white hover:text-charcoal-brown transition">Zaloguj się</button>
      </form>

      <p v-if="message" class="mt-4 text-center font-bold text-floral-white">{{ message }}</p>

      <router-link to="/register" class="mt-4 text-sm text-floral-white">Nie masz konta? <a class="text-spicy-paprika hover:underline">Zarejestruj się</a></router-link>
    </div>
  </div>
</template>