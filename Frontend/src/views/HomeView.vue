<script setup lang="ts">
</script>

<template>
  <div class="flex min-h-full flex-1 flex-col">
    <div class="flex flex-1 flex-col items-center w-full py-20 px-6 gap-24">

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

<section class="grid w-full max-w-6xl grid-cols-1 gap-6 md:grid-cols-3">
  <article
    class="group rounded-2xl border border-silver/70 bg-white p-7 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-spicy-paprika/50 hover:shadow-xl"
  >
    <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-spicy-paprika transition-colors group-hover:bg-spicy-paprika group-hover:text-white">
      <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
        <path stroke-linecap="round" stroke-linejoin="round" d="M12 6.5c-2-1.3-5-1.5-8-1.5v13c3 0 6 .2 8 1.5m0-13c2-1.3 5-1.5 8-1.5v13c-3 0-6 .2-8 1.5m0-13v13.5" />
      </svg>
    </div>
    <h3 class="mb-3 text-xl font-bold text-charcoal-brown">Bogate materiały</h3>
    <p class="leading-relaxed text-gray-600">
      Poznawaj Linuxa dzięki interaktywnym lekcjom, praktycznym zadaniom i quizom.
    </p>
  </article>

  <article
    class="group rounded-2xl border border-silver/70 bg-white p-7 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-spicy-paprika/50 hover:shadow-xl"
  >
    <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-spicy-paprika transition-colors group-hover:bg-spicy-paprika group-hover:text-white">
      <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
        <path stroke-linecap="round" stroke-linejoin="round" d="M4 19V5m0 14h16M8 15v-3m4 3V8m4 7v-5m4 5V6" />
      </svg>
    </div>
    <h3 class="mb-3 text-xl font-bold text-charcoal-brown">Śledzenie postępów</h3>
    <p class="leading-relaxed text-gray-600">
      Sprawdzaj swoje wyniki i łatwo zobacz, czego warto nauczyć się dalej.
    </p>
  </article>

  <article
    class="group rounded-2xl border border-silver/70 bg-white p-7 shadow-sm transition-all duration-300 hover:-translate-y-1 hover:border-spicy-paprika/50 hover:shadow-xl"
  >
    <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-spicy-paprika transition-colors group-hover:bg-spicy-paprika group-hover:text-white">
      <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
        <circle cx="12" cy="12" r="9" />
        <path stroke-linecap="round" stroke-linejoin="round" d="M12 7v5l3 2" />
      </svg>
    </div>
    <h3 class="mb-3 text-xl font-bold text-charcoal-brown">Nauka we własnym tempie</h3>
    <p class="leading-relaxed text-gray-600">
      Wracaj do kursów wtedy, kiedy chcesz -- wystarczy przeglądarka i dostęp do internetu.
    </p>
  </article>
</section>

    </div>

    <footer class="mt-auto bg-charcoal-brown text-floral-white py-6 shrink-0 z-10 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] rounded-4xl border-t border-silver/20 shadow-inner">
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
  </div>
</template>

<style scoped>
</style>