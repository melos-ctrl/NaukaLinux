<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router' // dodanie routera

const username = ref('')
const password = ref('')
const message = ref('')

const router = useRouter() // inicjalizacja routera

const handleLogin = async () => {
  try {
    // Uwaga: Endpoint /login musimy jeszcze napisać w C#!
    const response = await fetch('http://localhost:5042/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        username: username.value,
        password: password.value
      })
    })

    if (response.ok) {

      const data = await response.json()
      localStorage.setItem('token', data.token) // zakładamy, że backend zwraca token w polu

      router.push('/lesson')
    } else {
      message.value = 'Nieprawidłowy login lub hasło.'
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

        <button type="submit" class="bg-spicy-paprika text-floral-white px-4 py-2 rounded mt-4 hover:bg-blue-600 transition">Zaloguj się</button>
      </form>

      <p v-if="message" class="mt-4 text-center font-bold text-floral-white">{{ message }}</p>

      <span class="mt-4 text-sm text-floral-white">Nie masz konta? <a href="/register" class="text-spicy-paprika hover:underline">Zarejestruj się</a></span>
    </div>
  </div>
</template>
