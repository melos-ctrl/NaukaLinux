<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const token = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const message = ref('')
const isLoading = ref(false)

onMounted(() => {
  token.value = route.query.token as string || ''
  if (!token.value) {
    message.value = 'Brak tokena. Użyj linku przesłanego na e-mail.'
  }
})

const resetPassword = async () => {
  if (newPassword.value !== confirmPassword.value) {
    message.value = 'Hasła nie są identyczne!'
    return
  }

  isLoading.value = true
  message.value = ''

  try {
    const response = await fetch('/api/auth/reset-password', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ 
        token: token.value, 
        newPassword: newPassword.value 
      })
    })

    if (response.ok) {
      message.value = 'Hasło zmienione! Przenoszę do logowania...'
      setTimeout(() => router.push('/login'), 3000)
    } else {
      message.value = 'Nieprawidłowy lub przeterminowany token.'
    }
  } catch (error) {
    message.value = 'Błąd połączenia z serwerem.'
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div class="flex flex-col items-center justify-center h-screen">
    <div class="bg-carbon-black p-8 rounded shadow flex flex-col items-center w-full max-w-sm">
      <h1 class="text-3xl font-bold text-floral-white mb-2">Nowe hasło</h1>
      <p class="text-silver text-sm mb-6 text-center">Wpisz nowe hasło dla swojego konta.</p>

      <form @submit.prevent="resetPassword" class="flex flex-col w-full gap-2">
        <label for="newPassword" class="text-floral-white">Nowe hasło</label>
        <input 
          v-model="newPassword" 
          class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" 
          type="password" 
          id="newPassword" 
          required minlength="6"
        >

        <label for="confirmPassword" class="text-floral-white mt-2">Powtórz hasło</label>
        <input 
          v-model="confirmPassword" 
          class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" 
          type="password" 
          id="confirmPassword" 
          required minlength="6"
        >

        <button 
          type="submit" 
          :disabled="isLoading || !token"
          class="bg-spicy-paprika text-floral-white px-4 py-2 rounded mt-4 hover:bg-floral-white hover:text-charcoal-brown transition disabled:opacity-50"
        >
          {{ isLoading ? 'Zapisywanie...' : 'Zmień hasło' }}
        </button>
      </form>

      <p v-if="message" class="mt-4 text-center font-bold text-floral-white text-sm">{{ message }}</p>
    </div>
  </div>
</template>