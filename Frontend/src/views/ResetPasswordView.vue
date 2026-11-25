<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AuthCard from '@/components/AuthCard.vue'

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
  <AuthCard eyebrow="Reset hasła" title="Nowe hasło" lead="Wpisz nowe hasło dla swojego konta.">
    <form @submit.prevent="resetPassword" class="space-y-4">
      <div>
        <label for="newPassword" class="label">Nowe hasło</label>
        <input v-model="newPassword" class="input" type="password" id="newPassword" autocomplete="new-password" required minlength="6">
      </div>

      <div>
        <label for="confirmPassword" class="label">Powtórz hasło</label>
        <input v-model="confirmPassword" class="input" type="password" id="confirmPassword" autocomplete="new-password" required minlength="6">
      </div>

      <p v-if="message" class="alert alert-info" role="status">{{ message }}</p>

      <button type="submit" :disabled="isLoading || !token" class="btn btn-primary w-full">
        {{ isLoading ? 'Zapisywanie...' : 'Zmień hasło' }}
      </button>
    </form>

    <template #footer>
      <router-link to="/login" class="font-semibold text-paprika-600 hover:underline">← Wróć do logowania</router-link>
    </template>
  </AuthCard>
</template>
