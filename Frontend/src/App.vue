<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
// stan logowania użytkownika
const router = useRouter();
const route = useRoute();

const isLoggedIn = ref(!!localStorage.getItem('token'));

watch(() => route.path, () => {
  isLoggedIn.value = !!localStorage.getItem('token');
});

//obsługa menu rozwijanego
const isDropdownOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

const toggleDropdown = () => {
  isDropdownOpen.value = !isDropdownOpen.value;
};

// zamykanie menu rozwijanego po kliknięciu poza nim
const isDropdownClosed = (event: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isDropdownOpen.value = false;
  }
};

// wylogowywanie się na ten czas placeholder
const logout = () => {
  localStorage.removeItem('token');
  isLoggedIn.value = false;
  isDropdownOpen.value = false;

  router.push('/login');
}

// dodanie nasłuchiwania zdarzenia kliknięcia po zamontowaniu komponentu
onMounted(() => {
  document.addEventListener('click', isDropdownClosed)
});

// usunięcie nasłuchiwania zdarzenia kliknięcia po odmontowaniu komponentu
onUnmounted(() => {
  document.removeEventListener('click', isDropdownClosed)
});

</script>

<template>
<div class="flex flex-col h-screen overflow-hidden">
  <header class="flex items-center justify-between bg-charcoal-brown text-sm px-6 text-floral-white shrink-0 shadow-md z-10">
    <div class="flex gap-5 p-5">
      <router-link to="/" class="hover:text-spicy-paprika transition-colors">HOME</router-link>
      <router-link to="/lesson" class="hover:text-spicy-paprika transition-colors">LEKCJA</router-link>
      <router-link to="/courses" class="hover:text-spicy-paprika transition-colors">KURSY-TYMCZASOWA</router-link>
    </div>
  <div class="flex gap-5 p-5">
    <template v-if="!isLoggedIn">
      <router-link to="/login" class="px-6 py-2 bg-silver text-charcoal-brown rounded-md font-semibold hover:bg-floral-white transition-all shadow">Zaloguj się</router-link>
      <router-link to="/register" class="px-6 py-2 bg-spicy-paprika text-floral-white rounded-md font-semibold hover:opacity-90 transition-all shadow">Zarejestruj się</router-link>
    </template>

    <div v-else class="relative" ref="dropdownRef">
      <button @click="toggleDropdown" class="px-6 py-2 bg-silver text-charcoal-brown rounded-md font-semibold hover:bg-floral-white transition-all shadow">Profil</button>
      <div v-if="isDropdownOpen" class="absolute right-0 mt-2 w-48 bg-silver rounded-md shadow-lg z-20">
        <router-link to="/profile" class="block px-4 py-2 text-charcoal-brown hover:bg-floral-white transition-colors">Mój profil</router-link>
        <router-link to="/settings" class="block px-4 py-2 text-charcoal-brown hover:bg-floral-white transition-colors">Ustawienia</router-link>
        <button @click="logout" class="w-full text-left px-4 py-2 text-charcoal-brown hover:bg-floral-white transition-colors">Wyloguj się</button>
      </div>

    </div>
  </div>
  
  </header>

  <main class="flex-1 overflow-y-auto bg-floral-white">
    <router-view></router-view>
  </main>
  
  <footer>
  </footer>
</div>
</template>

<style scoped></style>
