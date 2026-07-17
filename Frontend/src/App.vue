<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const router = useRouter();
const route = useRoute();
const isLoggedIn = ref(!!localStorage.getItem('token'));

watch(() => route.path, () => {
  isLoggedIn.value = !!localStorage.getItem('token');
});

const isDropdownOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

const toggleDropdown = () => { isDropdownOpen.value = !isDropdownOpen.value; };

const isDropdownClosed = (event: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(event.target as Node)) {
    isDropdownOpen.value = false;
  }
};

const logout = () => {
  localStorage.removeItem('token');
  localStorage.removeItem('role');
  isLoggedIn.value = false;
  isDropdownOpen.value = false;
  router.push('/login');
}

onMounted(() => { document.addEventListener('click', isDropdownClosed); });
onUnmounted(() => { document.removeEventListener('click', isDropdownClosed); });
</script>

<template>
  <div class="flex flex-col h-screen overflow-hidden">
    <header class="flex items-center justify-between bg-charcoal-brown px-8 py-4 text-floral-white shrink-0 shadow-md z-10">
      <div class="flex items-center gap-8">
        <router-link to="/" class="text-xl font-extrabold flex items-center gap-2 hover:text-spicy-paprika transition-colors tracking-wide">
        NaukaLinux
        </router-link>
        <nav class="flex gap-6 font-medium text-sm border-l border-gray-600 pl-8">
          <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog Kursów</router-link>
        </nav>
      </div>

      <div class="flex items-center gap-5 text-sm">
        <template v-if="!isLoggedIn">
          <router-link to="/login" class="font-semibold text-floral-white hover:text-spicy-paprika transition-colors">
            Zaloguj się
          </router-link>
          <router-link to="/register" class="px-5 py-2 bg-spicy-paprika text-floral-white rounded-md font-bold shadow-sm hover:opacity-90 hover:shadow-md transition-all">
            Zarejestruj się
          </router-link>
        </template>
        <div v-else class="relative" ref="dropdownRef">
          <button @click="toggleDropdown" class="flex items-center gap-2 px-4 py-2 border border-silver rounded-md font-semibold hover:bg-silver hover:text-charcoal-brown transition-all">
            Mój profil <span class="text-xs text-gray-400">▼</span>
          </button>
          <div v-if="isDropdownOpen" class="absolute right-0 mt-3 w-56 bg-floral-white rounded-md shadow-xl z-20 border border-silver overflow-hidden">
            <div class="px-4 py-3 bg-gray-100 border-b border-gray-200">
              <span class="block text-xs text-gray-500 font-semibold uppercase">Menu użytkownika</span>
            </div>
            <router-link to="/dashboard" class="block px-4 py-3 text-charcoal-brown hover:bg-silver transition-colors font-medium">Panel użytkownika</router-link>
            <div class="border-t border-gray-200"></div>
            <button @click="logout" class="w-full text-left px-4 py-3 text-spicy-paprika hover:bg-gray-100 transition-colors font-bold">Wyloguj się</button>
          </div>
        </div>
      </div>
    </header>

    <main class="flex-1 overflow-y-auto bg-floral-white">
      <router-view></router-view>
    </main>
    <footer></footer>
  </div>
</template>