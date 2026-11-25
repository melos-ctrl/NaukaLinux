<script setup lang="ts">
</script>

<template>
  <div class="flex min-h-full flex-1 flex-col">
    <div class="flex w-full flex-1 flex-col items-center gap-24 px-6 py-20">

      <section class="flex w-full max-w-6xl flex-col items-center justify-center gap-12 md:flex-row md:gap-20">
        <div class="flex flex-1 flex-col items-center text-center md:items-start md:text-left">
          <h1 class="mb-10 font-mono text-5xl font-bold leading-tight text-carbon-black md:text-6xl">
            Nawet pingwina można nauczyć <span class="text-paprika-600">latać</span>
          </h1>

          <div class="flex w-full flex-col justify-center gap-4 sm:flex-row md:justify-start">
            <router-link to="/register" class="btn btn-primary h-14 px-8 text-lg">
              Dołącz za darmo
            </router-link>
            <router-link to="/courses" class="btn btn-outline h-14 px-8 text-lg">
              Wyszukaj kursy
            </router-link>
          </div>
        </div>

        <div class="flex flex-1 justify-center md:justify-end">
          <img
            src="/src/images/tux.svg"
            alt="Pingwin Tux"
            class="w-72 object-contain drop-shadow-2xl transition-transform duration-300 hover:scale-105 md:w-96"
          />
        </div>
      </section>

      <section class="grid w-full max-w-6xl grid-cols-1 gap-6 md:grid-cols-3">
        <article class="group card card-hover hover:border-spicy-paprika/50">
          <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-paprika-600 transition-colors group-hover:bg-paprika-600 group-hover:text-white">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 6.5c-2-1.3-5-1.5-8-1.5v13c3 0 6 .2 8 1.5m0-13c2-1.3 5-1.5 8-1.5v13c-3 0-6 .2-8 1.5m0-13v13.5" />
            </svg>
          </div>
          <h3 class="mb-3 section-title">Bogate materiały</h3>
          <p class="leading-relaxed text-muted">
            Poznawaj Linuxa dzięki interaktywnym lekcjom, praktycznym zadaniom i quizom.
          </p>
        </article>

        <article class="group card card-hover hover:border-spicy-paprika/50">
          <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-paprika-600 transition-colors group-hover:bg-paprika-600 group-hover:text-white">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 19V5m0 14h16M8 15v-3m4 3V8m4 7v-5m4 5V6" />
            </svg>
          </div>
          <h3 class="mb-3 section-title">Śledzenie postępów</h3>
          <p class="leading-relaxed text-muted">
            Sprawdzaj swoje wyniki i łatwo zobacz, czego warto nauczyć się dalej.
          </p>
        </article>

        <article class="group card card-hover hover:border-spicy-paprika/50">
          <div class="mb-6 flex h-12 w-12 items-center justify-center rounded-xl bg-spicy-paprika/10 text-paprika-600 transition-colors group-hover:bg-paprika-600 group-hover:text-white">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8" aria-hidden="true">
              <circle cx="12" cy="12" r="9" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 7v5l3 2" />
            </svg>
          </div>
          <h3 class="mb-3 section-title">Nauka we własnym tempie</h3>
          <p class="leading-relaxed text-muted">
            Wracaj do kursów wtedy, kiedy chcesz -- wystarczy przeglądarka i dostęp do internetu.
          </p>
        </article>
      </section>

    </div>

    <footer class="mt-auto shrink-0 border-t border-silver/20 bg-charcoal-brown py-6 text-floral-white">
      <div class="mx-auto flex max-w-7xl flex-col items-center justify-between gap-4 px-6 text-sm md:flex-row">
        <div class="text-silver">
          &copy; NaukaLinux 2026. Wszelkie prawa zastrzeżone.
        </div>
        <div class="flex flex-wrap justify-center gap-6 font-semibold">
          <a href="#" class="nav-link">O nas</a>
          <a href="#" class="nav-link">Kontakt</a>
          <a href="#" class="nav-link">Regulamin</a>
          <a href="#" class="nav-link">Polityka prywatności</a>
        </div>
      </div>
    </footer>
  </div>
</template>
