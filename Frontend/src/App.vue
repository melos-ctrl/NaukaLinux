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

    <!-- h-18 = 4.5rem - ta wysokość jest użyta w klasie .fullscreen-view w main.css -->
    <header
      class="relative z-[100] flex h-18 shrink-0 items-center justify-between gap-4 border-b border-silver/20 bg-charcoal-brown px-6 text-floral-white shadow-md md:px-8"
      :class="{ 'border-b-2 border-spicy-paprika': isLoggedIn && (userRole === 'mentor' || userRole === 'admin') }"
    >
      <!-- Lewa sekcja: Logo i Nawigacja -->
      <div class="flex min-w-0 items-center gap-8">
        <router-link to="/" class="nav-link flex items-center gap-2 font-mono text-xl font-bold">
          NaukaLinux
          <span v-if="isLoggedIn && userRole === 'mentor'" class="badge badge-primary font-sans">Mentor</span>
          <span v-else-if="isLoggedIn && userRole === 'admin'" class="badge badge-primary font-sans">Admin</span>
        </router-link>

        <nav class="flex gap-6 overflow-x-auto whitespace-nowrap border-l border-silver/25 pl-8 text-sm font-medium">
          <!-- Linki Mentora -->
          <template v-if="isLoggedIn && userRole === 'mentor'">
            <router-link to="/mentor-dashboard" class="nav-link" active-class="nav-link-active">Moje kursy</router-link>
            <router-link to="/course-creator" class="nav-link" active-class="nav-link-active">Kreator kursu</router-link>
            <router-link to="/courses" class="nav-link" active-class="nav-link-active">Katalog</router-link>
            <router-link to="/sandbox" class="nav-link" active-class="nav-link-active">Piaskownica</router-link>
          </template>

          <template v-else-if="isLoggedIn && userRole === 'admin'">
            <router-link to="/mentor-dashboard" class="nav-link" active-class="nav-link-active">Kursy</router-link>
            <router-link to="/course-creator" class="nav-link" active-class="nav-link-active">Kreator kursu</router-link>
            <router-link to="/admin" class="nav-link" active-class="nav-link-active">Panel admina</router-link>
            <router-link to="/admin?tab=mentorApplications" class="nav-link" active-class="nav-link-active">Zgłoszenia mentorów</router-link>
            <router-link to="/courses" class="nav-link" active-class="nav-link-active">Katalog</router-link>
          </template>

          <!-- Linki Użytkownika -->
          <template v-else-if="isLoggedIn">
            <router-link to="/dashboard" class="nav-link" active-class="nav-link-active">Mój profil</router-link>
            <router-link to="/courses" class="nav-link" active-class="nav-link-active">Katalog kursów</router-link>
            <router-link to="/sandbox" class="nav-link" active-class="nav-link-active">Piaskownica</router-link>
          </template>

          <!-- Linki Niezalogowanego -->
          <template v-else>
            <router-link to="/courses" class="nav-link" active-class="nav-link-active">Katalog kursów</router-link>
          </template>
        </nav>
      </div>

      <!-- Prawa sekcja: Akcje i Menu -->
      <div class="relative flex shrink-0 items-center gap-3 text-sm" ref="dropdownRef">

        <!-- Widok Niezalogowanego -->
        <template v-if="!isLoggedIn">
          <router-link to="/login" class="btn btn-sm btn-outline-dark">Zaloguj się</router-link>
          <router-link to="/register" class="btn btn-sm btn-primary">Zarejestruj się</router-link>
        </template>

        <!-- Widok Zalogowanego (Mentor i Użytkownik) -->
        <template v-else>
          <router-link
            v-if="userRole !== 'mentor' && userRole !== 'admin'"
            to="/be-a-mentor"
            class="btn btn-sm btn-primary"
          >
            Zostań mentorem
          </router-link>
          <button
            type="button"
            @click="toggleDropdown"
            class="btn btn-sm btn-outline-dark"
            :aria-expanded="isDropdownOpen"
          >
            {{ userRole === 'mentor' ? 'Panel mentora' : userRole === 'admin' ? 'Panel admina' : 'Mój profil' }}
            <span aria-hidden="true">▾</span>
          </button>

          <!-- Dropdown Menu -->
          <div v-if="isDropdownOpen" class="absolute right-0 top-full z-20 mt-3 w-56 overflow-hidden rounded-xl border border-line bg-white shadow-xl">
            <template v-if="userRole === 'admin'">
              <router-link to="/admin" class="block px-4 py-3 font-medium text-carbon-black transition-colors hover:bg-floral-white" @click="isDropdownOpen = false">
                Panel administratora
              </router-link>
              <div class="border-t border-line"></div>
            </template>
            <template v-else-if="userRole === 'mentor'">
              <router-link to="/mentor-dashboard" class="block px-4 py-3 font-medium text-carbon-black transition-colors hover:bg-floral-white" @click="isDropdownOpen = false">
                Panel mentora
              </router-link>
              <div class="border-t border-line"></div>
            </template>
            <template v-else>
              <router-link to="/dashboard" class="block px-4 py-3 font-medium text-carbon-black transition-colors hover:bg-floral-white" @click="isDropdownOpen = false">
                Panel użytkownika
              </router-link>
              <router-link to="/settings" class="block px-4 py-3 font-medium text-carbon-black transition-colors hover:bg-floral-white" @click="isDropdownOpen = false">
                Ustawienia
              </router-link>
              <div class="border-t border-line"></div>
            </template>

            <!-- Wspólny przycisk wylogowania -->
            <button type="button" @click="logout" class="w-full px-4 py-3 text-left font-semibold text-red-700 transition-colors hover:bg-red-50">
              Wyloguj się
            </button>
          </div>
        </template>

      </div>
    </header>

    <main class="flex min-w-0 flex-1 flex-col bg-floral-white">
      <router-view></router-view>
    </main>
  </div>
</template>
