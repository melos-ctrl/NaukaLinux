<script setup lang="ts">
import { computed, ref } from 'vue'
import { starsLabel } from '@/utils/plural'

// Gwiazdki: w trybie readonly pokazują ocenę (także ułamkową, np. średnią 4.3),
// w trybie edycji pozwalają wybrać od 1 do 5 (myszką albo klawiaturą: Tab + Enter/Spacja).
const props = withDefaults(defineProps<{
  modelValue?: number | null
  readonly?: boolean
  size?: 'sm' | 'md' | 'lg'
}>(), {
  modelValue: 0,
  readonly: false,
  size: 'md'
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: number): void
}>()

const STARS = [1, 2, 3, 4, 5]

const hovered = ref(0)
const shownValue = computed(() =>
  !props.readonly && hovered.value > 0 ? hovered.value : (props.modelValue ?? 0)
)

// Ile procent danej gwiazdki wypełnić: przy średniej 4.3 czwarta ma 100%, piąta 30%
const fillPercent = (star: number) =>
  Math.round(Math.min(1, Math.max(0, shownValue.value - (star - 1))) * 100)

const sizeClass = computed(() => ({
  sm: 'text-base',
  md: 'text-2xl',
  lg: 'text-4xl'
}[props.size]))

const readonlyLabel = computed(() =>
  `Ocena ${(props.modelValue ?? 0).toLocaleString('pl-PL', { maximumFractionDigits: 1 })} na 5`
)
</script>

<template>
  <span
    v-if="readonly"
    class="inline-flex select-none leading-none"
    :class="sizeClass"
    role="img"
    :aria-label="readonlyLabel"
  >
    <!-- Szara gwiazdka pod spodem + złota nakładka przycięta do procentu wypełnienia -->
    <span v-for="star in STARS" :key="star" class="relative inline-block text-silver/40" aria-hidden="true">★<span
        class="absolute left-0 top-0 h-full overflow-hidden whitespace-nowrap text-amber-400"
        :style="{ width: fillPercent(star) + '%' }"
      >★</span></span>
  </span>

  <span
    v-else
    class="inline-flex leading-none"
    :class="sizeClass"
    role="radiogroup"
    aria-label="Twoja ocena"
    @mouseleave="hovered = 0"
  >
    <button
      v-for="star in STARS"
      :key="star"
      type="button"
      role="radio"
      :aria-checked="modelValue === star"
      :aria-label="starsLabel(star)"
      :title="starsLabel(star)"
      class="rounded px-0.5 transition-transform hover:scale-110 focus:outline-none focus-visible:ring-2 focus-visible:ring-spicy-paprika"
      :class="star <= shownValue ? 'text-amber-400' : 'text-silver/40'"
      @mouseenter="hovered = star"
      @focus="hovered = star"
      @blur="hovered = 0"
      @click="emit('update:modelValue', star)"
    >★</button>
  </span>
</template>
