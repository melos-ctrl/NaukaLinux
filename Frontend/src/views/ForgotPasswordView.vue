<script setup lang="ts">
import { ref } from 'vue'

const email = ref('')
const message = ref('')
const isLoading = ref(false)

const submitRequest = async () => {
    isLoading.value = true
    message.value = ''
    
    try {
        const response = await fetch('/api/auth/forgot-password', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                email: email.value
            })
        })

        const data = await response.json()
        message.value = data.message || 'Jeśli e-mail istnieje w naszej bazie, wysłaliśmy link do resetowania hasła.'
    } catch (error) {
        message.value = 'Wystąpił błąd podczas wysyłania żądania. Spróbuj ponownie później.'
    } finally {
        isLoading.value = false
    }
}
</script>

<template>
  <div class="flex flex-col items-center justify-center h-screen">
    <div class="bg-carbon-black p-8 rounded shadow flex flex-col items-center w-full max-w-sm">
      <h1 class="text-3xl font-bold text-floral-white mb-2">Odzyskaj hasło</h1>
      <p class="text-silver text-sm mb-6 text-center">Podaj swój e-mail, a wyślemy Ci link do zmiany hasła.</p>

      <form @submit.prevent="submitRequest" class="flex flex-col w-full gap-2">
        <label for="email" class="text-floral-white">Adres e-mail</label>
        <input 
          v-model="email" 
          class="bg-carbon-black border border-silver rounded py-2 px-4 focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-floral-white" 
          type="email" 
          id="email" 
          required
        >

        <button 
          type="submit" 
          :disabled="isLoading"
          class="bg-spicy-paprika text-floral-white px-4 py-2 rounded mt-4 hover:bg-floral-white hover:text-charcoal-brown transition disabled:opacity-50"
        >
          {{ isLoading ? 'Wysyłanie...' : 'Wyślij link' }}
        </button>
      </form>

      <p v-if="message" class="mt-4 text-center font-bold text-floral-white text-sm">{{ message }}</p>

      <span class="mt-4 text-sm text-floral-white">
        <router-link to="/login" class="text-spicy-paprika hover:underline">Wróć do logowania</router-link>
      </span>
    </div>
  </div>
</template>