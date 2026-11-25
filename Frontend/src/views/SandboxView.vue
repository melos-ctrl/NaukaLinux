<script setup lang="ts">
import RemoteDesktop from '@/components/RemoteDesktop.vue'
import PageHeader from '@/components/PageHeader.vue'
</script>

<template>
  <div class="page max-w-7xl">
    <PageHeader
      eyebrow="Piaskownica"
      title="Piaskownica Linux"
      lead="Uruchom pełne środowisko Linux i ćwicz bez wpływu na swój komputer."
    />

    <RemoteDesktop />
  </div>
</template>
