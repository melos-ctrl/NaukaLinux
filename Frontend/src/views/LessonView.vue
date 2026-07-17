<template>
  <div class="flex h-screen w-full overflow-hidden">

    <div class="flex-1 bg-slate-800 text-white flex flex-col items-center justify-center p-8">
      <h2 class="text-2xl font-semibold mb-4">Zadanie 1</h2>

      <p class="text-slate-400 leading-relaxed text-center max-w-md mb-6">
        Twoim zadaniem jest zalogowanie się do terminala obok i sprawdzenie wersji jądra systemu.
      </p>

      <div class="p-4 bg-black rounded border border-slate-700 font-mono text-sm text-green-500 w-full max-w-md text-center shadow-lg">
        $ uname -a
      </div>
    </div>

    <div class="flex-1 bg-slate-100 flex flex-col items-center justify-center overflow-auto">
        <RemoteDesktop />
    </div>

  </div>
</template>

<script setup lang="ts">
import RemoteDesktop from '../components/RemoteDesktop.vue';
</script>

<style scoped></style>
