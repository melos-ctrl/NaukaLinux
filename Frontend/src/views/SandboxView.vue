<script setup lang="ts">
import RemoteDesktop from '@/components/RemoteDesktop.vue'
</script>

<template>
  <section class="mx-auto w-full max-w-7xl">
    <header class="px-5 pt-6 text-center">
      <h1 class="text-3xl font-extrabold text-charcoal-brown">Piaskownica Linux</h1>
      <p class="mt-2 text-charcoal-brown/70">
        Uruchom pełne środowisko Linux i ćwicz bez wpływu na swój komputer.
      </p>
    </header>

    <RemoteDesktop />
  </section>
</template>
