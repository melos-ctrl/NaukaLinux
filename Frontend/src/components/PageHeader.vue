<script setup lang="ts">
defineProps<{
  eyebrow?: string
  title: string
  lead?: string
}>()
</script>

<template>
  <header class="flex flex-wrap items-end justify-between gap-4">
    <div>
      <p v-if="eyebrow" class="eyebrow">{{ eyebrow }}</p>
      <h1 class="page-title">{{ title }}</h1>
      <p v-if="lead" class="page-lead">{{ lead }}</p>
    </div>
    <!-- miejsce na przycisk akcji, np. "+ Stwórz nowy kurs" -->
    <div v-if="$slots.actions" class="flex flex-wrap gap-2">
      <slot name="actions" />
    </div>
  </header>
</template>
