<script setup lang="ts">
import { ref } from 'vue'
import AuthCard from '@/components/AuthCard.vue'

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
  <AuthCard eyebrow="Reset hasła" title="Odzyskaj hasło" lead="Podaj swój e-mail, a wyślemy Ci link do zmiany hasła.">
    <form @submit.prevent="submitRequest" class="space-y-4">
      <div>
        <label for="email" class="label">Adres e-mail</label>
        <input v-model="email" class="input" type="email" id="email" autocomplete="email" required>
      </div>

      <p v-if="message" class="alert alert-info" role="status">{{ message }}</p>

      <button type="submit" :disabled="isLoading" class="btn btn-primary w-full">
        {{ isLoading ? 'Wysyłanie...' : 'Wyślij link' }}
      </button>
    </form>

    <template #footer>
      <router-link to="/login" class="font-semibold text-paprika-600 hover:underline">← Wróć do logowania</router-link>
    </template>
  </AuthCard>
</template>
