<script setup lang="ts">
import { computed } from 'vue'
import { renderMarkdown } from '@/utils/markdown'

const props = defineProps<{
  source?: string | null
  /** true = jasny tekst na ciemnym tle (panel lekcji) */
  dark?: boolean
}>()

const html = computed(() => renderMarkdown(props.source))
</script>

<template>
  <!-- v-html jest tu bezpieczne, bo renderMarkdown czyści HTML przez DOMPurify -->
  <div class="markdown" :class="{ 'markdown-dark': dark }" v-html="html"></div>
</template>
