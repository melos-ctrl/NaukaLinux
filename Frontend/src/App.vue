<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';

const router = useRouter();
const route = useRoute();
const isLoggedIn = ref(!!localStorage.getItem('token'));
const userRole = ref((localStorage.getItem('role') || '').toLowerCase());

watch(() => route.path, () => {
  isLoggedIn.value = !!localStorage.getItem('token');
  userRole.value = (localStorage.getItem('role') || '').toLowerCase();
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
  userRole.value = '';
  isDropdownOpen.value = false;
  router.push('/login');
  localStorage.removeItem('activeContainerId');
  localStorage.removeItem('desktopUrl');
}

onMounted(() => { document.addEventListener('click', isDropdownClosed); });
onUnmounted(() => { document.removeEventListener('click', isDropdownClosed); });
</script>

<template>
  <div class="flex flex-col h-screen overflow-hidden">
    
    <header v-if="isLoggedIn && userRole === 'mentor'" class="flex items-center justify-between bg-charcoal-brown px-8 py-4 text-floral-white shrink-0 shadow-md relative z-[100] border-b-2 border-spicy-paprika">
      <div class="flex items-center gap-8">
        <router-link to="/" class="text-xl font-extrabold flex items-center gap-2 hover:text-spicy-paprika transition-colors tracking-wide">
          NaukaLinux <span class="text-[10px] bg-spicy-paprika text-white px-2 py-0.5 rounded ml-1 uppercase tracking-widest">Mentor</span>
        </router-link>
        <nav class="flex gap-6 font-medium text-sm border-l border-gray-600 pl-8">
          <router-link to="/mentor-dashboard" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Moje Kursy</router-link>
          <router-link to="/course-creator" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Kreator Kursu</router-link>
          <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog</router-link>
        </nav>
      </div>
      <div class="flex items-center gap-5 text-sm relative" ref="dropdownRef">
        <button @click="toggleDropdown" class="flex items-center gap-2 px-4 py-2 bg-spicy-paprika text-white border border-spicy-paprika rounded-md font-semibold hover:bg-opacity-90 transition-all">
          Panel Mentora
        </button>
        <div v-if="isDropdownOpen" class="absolute right-0 top-full mt-3 w-56 bg-floral-white rounded-md shadow-xl z-20 border border-silver overflow-hidden">
          <button @click="logout" class="w-full text-left px-4 py-3 text-spicy-paprika hover:bg-gray-100 transition-colors font-bold">Wyloguj się</button>
        </div>
      </div>
    </header>

    <header v-else-if="isLoggedIn && userRole !== 'mentor'" class="flex items-center justify-between bg-charcoal-brown px-8 py-4 text-floral-white shrink-0 shadow-md relative z-[100]">
      <div class="flex items-center gap-8">
        <router-link to="/" class="text-xl font-extrabold flex items-center gap-2 hover:text-spicy-paprika transition-colors tracking-wide">
          NaukaLinux
        </router-link>
        <nav class="flex gap-6 font-medium text-sm border-l border-gray-600 pl-8">
          <router-link to="/dashboard" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Mój profil</router-link>
          <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog Kursów</router-link>
        </nav>
      </div>
      <div class="flex items-center gap-5 text-sm relative" ref="dropdownRef">
        <button @click="toggleDropdown" class="flex items-center gap-2 px-4 py-2 bg-floral-white text-charcoal-brown hover:bg-spicy-paprika border border-silver rounded-md font-semibold hover:text-floral-white transition-all">
          Mój profil
        </button>
        <div v-if="isDropdownOpen" class="absolute right-0 top-full mt-3 w-56 bg-floral-white rounded-md shadow-xl z-20 border border-silver overflow-hidden">
          <router-link to="/dashboard" class="block px-4 py-3 text-charcoal-brown hover:bg-silver transition-colors font-medium" @click="isDropdownOpen = false">Panel użytkownika</router-link>
          <div class="border-t border-gray-200"></div>
          <button @click="logout" class="w-full text-left px-4 py-3 text-spicy-paprika hover:bg-gray-100 transition-colors font-bold">Wyloguj się</button>
        </div>
      </div>
    </header>

    <header v-else class="flex items-center justify-between bg-charcoal-brown px-8 py-4 text-floral-white shrink-0 shadow-md relative z-[100]">
      <div class="flex items-center gap-8">
        <router-link to="/" class="text-xl font-extrabold flex items-center gap-2 hover:text-spicy-paprika transition-colors tracking-wide">
          NaukaLinux
        </router-link>
        <nav class="flex gap-6 font-medium text-sm border-l border-gray-600 pl-8">
          <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog Kursów</router-link>
        </nav>
      </div>
      <div class="flex items-center gap-5 text-sm">
        <router-link to="/login" class="font-semibold text-floral-white hover:text-spicy-paprika transition-colors">
          Zaloguj się
        </router-link>
        <router-link to="/register" class="px-5 py-2 bg-spicy-paprika text-floral-white rounded-md font-bold shadow-sm hover:opacity-90 hover:shadow-md transition-all">
          Zarejestruj się
        </router-link>
      </div>
    </header>

    <main class="flex-1 overflow-y-auto bg-floral-white">
      <router-view></router-view>
    </main>
    <footer></footer>
  </div>
</template>