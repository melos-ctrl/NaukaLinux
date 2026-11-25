<script setup lang="ts">
import { nextTick, ref } from 'vue'
import MarkdownContent from './MarkdownContent.vue'

const model = defineModel<string | undefined>()

defineProps<{
  placeholder?: string
  ariaLabel?: string
}>()

const mode = ref<'edit' | 'preview'>('edit')
const textarea = ref<HTMLTextAreaElement | null>(null)

const setValue = (value: string, selectFrom: number, selectTo: number) => {
  model.value = value
  void nextTick(() => {
    textarea.value?.focus()
    textarea.value?.setSelectionRange(selectFrom, selectTo)
  })
}

// Otacza zaznaczony tekst znacznikami, np. **pogrubienie**
const wrap = (before: string, after = before, placeholder = 'tekst') => {
  const el = textarea.value
  if (!el) return
  const value = model.value ?? ''
  const start = el.selectionStart
  const end = el.selectionEnd
  const selected = value.slice(start, end) || placeholder
  const next = value.slice(0, start) + before + selected + after + value.slice(end)
  setValue(next, start + before.length, start + before.length + selected.length)
}

// Dodaje prefiks na początku każdej zaznaczonej linii, np. "- " dla listy
const prefixLines = (prefix: string) => {
  const el = textarea.value
  if (!el) return
  const value = model.value ?? ''
  const lineStart = value.lastIndexOf('\n', el.selectionStart - 1) + 1
  const end = el.selectionEnd
  const block = value.slice(lineStart, end) || 'element'
  const prefixed = block.split('\n').map(line => prefix + line).join('\n')
  const next = value.slice(0, lineStart) + prefixed + value.slice(end)
  setValue(next, lineStart, lineStart + prefixed.length)
}

const tools = [
  { label: 'B', title: 'Pogrubienie (Ctrl+B)', cls: 'font-bold', run: () => wrap('**') },
  { label: 'I', title: 'Kursywa (Ctrl+I)', cls: 'italic', run: () => wrap('*') },
  { label: 'H', title: 'Nagłówek', cls: 'font-bold', run: () => prefixLines('### ') },
  { label: '`kod`', title: 'Kod w linii', cls: 'font-mono', run: () => wrap('`', '`', 'ls -la') },
  { label: '```', title: 'Blok kodu', cls: 'font-mono', run: () => wrap('\n```bash\n', '\n```\n', 'ls -la') },
  { label: '• lista', title: 'Lista punktowana', cls: '', run: () => prefixLines('- ') },
  { label: '1. lista', title: 'Lista numerowana', cls: '', run: () => prefixLines('1. ') },
  { label: 'link', title: 'Link', cls: 'underline', run: () => wrap('[', '](https://)', 'opis linku') },
]

const onKeydown = (event: KeyboardEvent) => {
  if (!(event.ctrlKey || event.metaKey)) return
  if (event.key === 'b') { event.preventDefault(); wrap('**') }
  if (event.key === 'i') { event.preventDefault(); wrap('*') }
}
</script>

<template>
  <div class="overflow-hidden rounded-lg border border-line-strong bg-white focus-within:border-spicy-paprika focus-within:ring-3 focus-within:ring-spicy-paprika/20">
    <!-- pasek: zakładki + narzędzia -->
    <div class="flex flex-wrap items-center justify-between gap-2 border-b border-line bg-floral-white px-2 py-1.5">
      <div class="flex gap-1" role="tablist">
        <button
          type="button"
          role="tab"
          :aria-selected="mode === 'edit'"
          class="rounded-md px-3 py-1.5 text-sm font-semibold transition-colors"
          :class="mode === 'edit' ? 'bg-white text-carbon-black shadow-sm' : 'text-muted hover:text-carbon-black'"
          @click="mode = 'edit'"
        >Edytuj</button>
        <button
          type="button"
          role="tab"
          :aria-selected="mode === 'preview'"
          class="rounded-md px-3 py-1.5 text-sm font-semibold transition-colors"
          :class="mode === 'preview' ? 'bg-white text-carbon-black shadow-sm' : 'text-muted hover:text-carbon-black'"
          @click="mode = 'preview'"
        >Podgląd</button>
      </div>

      <div v-if="mode === 'edit'" class="flex flex-wrap gap-0.5">
        <button
          v-for="tool in tools"
          :key="tool.title"
          type="button"
          :title="tool.title"
          :aria-label="tool.title"
          class="h-8 min-w-8 rounded-md px-2 text-sm text-charcoal-brown transition-colors hover:bg-white hover:text-paprika-600"
          :class="tool.cls"
          @click="tool.run"
        >{{ tool.label }}</button>
      </div>
    </div>

    <textarea
      v-show="mode === 'edit'"
      ref="textarea"
      v-model="model"
      rows="6"
      :placeholder="placeholder"
      :aria-label="ariaLabel"
      class="block w-full resize-y bg-white px-3.5 py-3 font-mono text-sm text-carbon-black outline-none placeholder:text-placeholder"
      @keydown="onKeydown"
    ></textarea>

    <div v-if="mode === 'preview'" class="min-h-36 px-4 py-3">
      <MarkdownContent v-if="model?.trim()" :source="model" />
      <p v-else class="text-sm text-muted">Nic tu jeszcze nie ma.</p>
    </div>

    <p class="border-t border-line bg-floral-white px-3 py-1.5 text-xs text-muted">
      Obsługuje Markdown: **pogrubienie**, *kursywa*, `kod`, ```bloki kodu```, listy i linki.
    </p>
  </div>
</template>
