<script setup lang="ts">
</script>

<template>
  <div class="flex flex-col items-center w-full py-20 px-6 gap-24">

    <section class="flex flex-col md:flex-row items-center justify-center max-w-6xl gap-12 md:gap-20 w-full">
      <div class="flex flex-col items-center md:items-start text-center md:text-left flex-1">
        <h1 class="text-5xl md:text-7xl font-extrabold text-charcoal-brown mb-10 leading-tight">
          Nawet pingwina można nauczyć <span class="text-spicy-paprika">latać</span>
        </h1>
        
        <div class="flex flex-col sm:flex-row gap-5 w-full justify-center md:justify-start">
          <router-link to="/register" class="px-8 py-4 bg-spicy-paprika text-floral-white rounded-lg font-bold text-lg shadow-md hover:opacity-90 hover:-translate-y-1 transition-all text-center">
            Dołącz za darmo
          </router-link>
          <router-link to="/courses" class="px-8 py-4 bg-white border-2 border-silver text-charcoal-brown rounded-lg font-bold text-lg shadow-sm hover:bg-gray-50 hover:-translate-y-1 transition-all text-center">
            Wyszukaj kursy
          </router-link>
        </div>
      </div>

      <div class="flex-1 flex justify-center md:justify-end">
        <img 
          src="/src/images/tux.svg" 
          alt="Pingwin Tux" 
          class="w-72 md:w-96 object-contain drop-shadow-2xl hover:scale-105 transition-transform duration-300" 
        />
      </div>
    </section>

    <section class="w-full max-w-6xl grid grid-cols-1 md:grid-cols-3 gap-10">
      <div class="bg-white p-8 rounded-xl shadow-sm border border-silver flex flex-col items-center text-center hover:shadow-md transition-shadow">
        <h3 class="text-xl font-bold text-charcoal-brown mb-3">Bogate Materiały</h3>
        <p class="text-gray-600">Dostęp do setek interaktywnych lekcji i zadań, które pomogą Ci opanować materiał.</p>
      </div>

      <div class="bg-white p-8 rounded-xl shadow-sm border border-silver flex flex-col items-center text-center hover:shadow-md transition-shadow">
        <h3 class="text-xl font-bold text-charcoal-brown mb-3">Śledzenie Postępów</h3>
        <p class="text-gray-600">Twój profil automatycznie zapisuje wyniki, pokazując na wykresach czego musisz się jeszcze douczyć.</p>
      </div>

      <div class="bg-white p-8 rounded-xl shadow-sm border border-silver flex flex-col items-center text-center hover:shadow-md transition-shadow">
        <h3 class="text-xl font-bold text-charcoal-brown mb-3">Dostęp 24/7</h3>
        <p class="text-gray-600">Ucz się gdzie chcesz i kiedy chcesz, z poziomu przeglądarki na komputerze oraz telefonie.</p>
      </div>
    </section>

  </div>

  <footer class="bg-charcoal-brown text-floral-white py-6 shrink-0 z-10 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] mt-auto">
    <div class="max-w-7xl mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-4 text-sm">
      <div class="text-silver">
        &copy; NaukaLinux 2026. Wszelkie prawa zastrzeżone.
      </div>
      <div class="flex gap-6 font-semibold">
        <a href="#" class="hover:text-spicy-paprika transition-colors">O nas</a>
        <a href="#" class="hover:text-spicy-paprika transition-colors">Kontakt</a>
        <a href="#" class="hover:text-spicy-paprika transition-colors">Regulamin</a>
        <a href="#" class="hover:text-spicy-paprika transition-colors">Polityka Prywatności</a>
      </div>
    </div>
  </footer>
</template>

<style scoped>
</style>