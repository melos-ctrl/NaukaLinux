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
  <div class="flex min-h-screen flex-col">
    
 <header 
  class="flex items-center justify-between bg-charcoal-brown px-8 py-4 text-floral-white shrink-0 shadow-md relative z-[100] rounded-b-2xl border-b border-silver/20 shadow-inner"
  :class="{ 'border-b-2 border-spicy-paprika': isLoggedIn && userRole === 'mentor' }"
>
  <!-- Lewa sekcja: Logo i Nawigacja -->
  <div class="flex items-center gap-8">
    <router-link to="/" class="text-xl font-extrabold flex items-center gap-2 hover:text-spicy-paprika transition-colors tracking-wide">
      NaukaLinux
      <span v-if="isLoggedIn && userRole === 'mentor'" class="text-[10px] bg-spicy-paprika text-white px-2 py-0.5 rounded ml-1 uppercase tracking-widest">
        Mentor
      </span>
    </router-link>

    <nav class="flex gap-6 font-medium text-sm border-l border-gray-600 pl-8">
      <!-- Linki Mentora -->
      <template v-if="isLoggedIn && userRole === 'mentor'">
        <router-link to="/mentor-dashboard" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Moje Kursy</router-link>
        <router-link to="/course-creator" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Kreator Kursu</router-link>
        <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog</router-link>
        <router-link to="/sandbox" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Piaskownica</router-link>
      </template>

      <!-- Linki Użytkownika -->
      <template v-else-if="isLoggedIn">
        <router-link to="/dashboard" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Mój profil</router-link>
        <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog Kursów</router-link>
        <router-link to="/sandbox" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Piaskownica</router-link>
      </template>

      <!-- Linki Niezalogowanego -->
      <template v-else>
        <router-link to="/courses" class="hover:text-spicy-paprika transition-colors" active-class="text-spicy-paprika font-bold">Katalog Kursów</router-link>
      </template>
    </nav>
  </div>

  <!-- Prawa sekcja: Akcje i Menu -->
  <div class="flex items-center gap-5 text-sm relative" ref="dropdownRef">
    
    <!-- Widok Niezalogowanego -->
    <template v-if="!isLoggedIn">
      <router-link to="/login" class="font-semibold text-floral-white hover:text-spicy-paprika transition-colors">
        Zaloguj się
      </router-link>
      <router-link to="/register" class="px-5 py-2 bg-spicy-paprika text-floral-white rounded-md font-bold shadow-sm hover:opacity-90 hover:shadow-md transition-all">
        Zarejestruj się
      </router-link>
    </template>

    <!-- Widok Zalogowanego (Mentor i Użytkownik) -->
    <template v-else>
      <button 
        @click="toggleDropdown" 
        class="flex items-center gap-2 px-4 py-2 rounded-md font-semibold transition-all border"
        :class="userRole === 'mentor' 
          ? 'bg-spicy-paprika text-white border-spicy-paprika hover:bg-opacity-90' 
          : 'bg-floral-white text-charcoal-brown hover:bg-spicy-paprika border-silver hover:text-floral-white'"
      >
        {{ userRole === 'mentor' ? 'Panel Mentora' : 'Mój profil' }}
      </button>

      <!-- Dropdown Menu -->
      <div v-if="isDropdownOpen" class="absolute right-0 top-full mt-3 w-56 bg-floral-white rounded-md shadow-xl z-20 border border-silver overflow-hidden">
        <!-- Opcje specyficzne dla zwykłego użytkownika -->
        <template v-if="userRole !== 'mentor'">
          <router-link to="/dashboard" class="block px-4 py-3 text-charcoal-brown hover:bg-silver transition-colors font-medium" @click="isDropdownOpen = false">
            Panel użytkownika
          </router-link>
          <div class="border-t border-gray-200"></div>
        </template>
        
        <!-- Wspólny przycisk wylogowania -->
        <button @click="logout" class="w-full text-left px-4 py-3 text-spicy-paprika hover:bg-gray-100 transition-colors font-bold">
          Wyloguj się
        </button>
      </div>
    </template>

  </div>
</header>

    <main class="flex min-w-0 flex-1 flex-col bg-floral-white p-4 ">
      <router-view></router-view>
    </main>
  </div>
</template>