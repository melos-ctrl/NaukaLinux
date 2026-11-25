<script setup lang="ts">
import { ref } from 'vue'
import { Turnstile } from '@sctg/turnstile-vue3'
import AuthCard from '@/components/AuthCard.vue'

const email = ref('')
const username = ref('')
const password = ref('')
const confirmPassword = ref('')
const message = ref('')
const registrationComplete = ref(false)
// Token do przechowywania tokenu Turnstile, używany do weryfikacji rejestracji.
const turnstileToken = ref('')
const turnstileSiteKey = import.meta.env.VITE_TURNSTILE_SITE_KEY as string
// Token Turnstile jest jednorazowy - po każdej próbie zmieniamy klucz,
// co tworzy widget od nowa i generuje świeży token.
const turnstileKey = ref(0)

const resetTurnstile = () => {
  turnstileToken.value = ''
  turnstileKey.value++
}

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
        cfToken: turnstileToken.value // Token Turnstile sprawdzany przez backend w Cloudflare
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
      resetTurnstile() // zużyty token - potrzebny nowy do kolejnej próby
    }
  } catch (error) {
    console.error(error)
    registrationComplete.value = false
    message.value = 'Błąd połączenia z serwerem.'
    resetTurnstile()
  }
}
</script>

<template>
  <AuthCard eyebrow="Nowe konto" title="Rejestracja" lead="Załóż konto i zacznij naukę Linuxa.">
    <form @submit.prevent="handleRegister" class="space-y-4">
      <div>
        <label for="email" class="label">E-mail</label>
        <input v-model="email" class="input" type="email" id="email" autocomplete="email" required>
      </div>

      <div>
        <label for="username" class="label">Nazwa użytkownika</label>
        <input v-model="username" class="input" type="text" id="username" autocomplete="username" required>
      </div>

      <div>
        <label for="password" class="label">Hasło</label>
        <input v-model="password" class="input" type="password" id="password" autocomplete="new-password" required>
      </div>

      <div>
        <label for="confirmPassword" class="label">Powtórz hasło</label>
        <input v-model="confirmPassword" class="input" type="password" id="confirmPassword" autocomplete="new-password" required>
      </div>

      <!-- Widget Turnstile -->
      <div class="flex justify-center">
        <Turnstile
          :key="turnstileKey"
          :siteKey="turnstileSiteKey"
          v-model="turnstileToken"
        />
      </div>

      <p
        v-if="message"
        class="alert"
        :class="registrationComplete ? 'alert-success' : 'alert-error'"
        :role="registrationComplete ? 'status' : 'alert'"
      >{{ message }}</p>

      <router-link
        v-if="registrationComplete"
        :to="{ name: 'resend-verification', query: { email } }"
        class="block text-center text-sm font-medium text-paprika-600 hover:underline"
      >Nie dostałeś maila? Wyślij link ponownie</router-link>

      <button type="submit" class="btn btn-primary w-full">Zarejestruj się</button>
    </form>

    <template #footer>
      Masz już konto?
      <router-link to="/login" class="font-semibold text-paprika-600 hover:underline">Zaloguj się</router-link>
    </template>
  </AuthCard>
</template>
