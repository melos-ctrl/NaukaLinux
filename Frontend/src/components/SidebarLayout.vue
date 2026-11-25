<script setup lang="ts">

</script>

<template>
  <div class="flex flex-1 flex-col gap-6 p-4 md:flex-row md:p-6">
    <aside
      class="flex w-full shrink-0 flex-col self-start overflow-hidden rounded-2xl bg-charcoal-brown text-floral-white shadow-lg md:w-64"
    >
      <section
        v-if="$slots.profile"
        class="flex flex-col items-center gap-2 border-b border-silver/20 p-6 text-center"
      >
        <slot name="profile" />
      </section>

      <nav class="flex flex-row gap-1 overflow-x-auto p-3 md:flex-col">
        <slot name="nav" />
      </nav>
    </aside>

    <main class="min-w-0 flex-1 space-y-8 px-1 py-2 md:px-4">
      <slot />
    </main>
  </div>
</template>
