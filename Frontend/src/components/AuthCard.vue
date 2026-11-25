<script setup lang="ts">
defineProps<{
  eyebrow?: string
  title: string
  lead?: string
}>()
</script>

<template>
  <div class="flex flex-1 items-center justify-center px-4 py-12">
    <div class="card w-full max-w-md space-y-6 md:p-9">
      <header class="text-center">
        <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
        <h1 class="page-title">{{ title }}</h1>
        <p v-if="lead" class="page-lead text-[15px]">{{ lead }}</p>
      </header>

      <slot />

      <div v-if="$slots.footer" class="text-center text-sm text-muted">
        <slot name="footer" />
      </div>
    </div>
  </div>
</template>
