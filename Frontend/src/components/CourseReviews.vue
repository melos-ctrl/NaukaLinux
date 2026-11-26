<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import axios from 'axios'
import StarRating from './StarRating.vue'
import { reviewsLabel } from '@/utils/plural'

// Oceny kursu: średnia, rozkład gwiazdek, formularz własnej oceny i lista opinii.
// Komponent jest w ciemnym motywie (tło charcoal-brown) - pasuje do widoku lekcji i modali.

interface Review {
  id: number
  username: string
  avatarUrl?: string | null
  rating: number
  comment: string
  createdAt: string
  updatedAt: string
  isMine: boolean
}

interface MyReview {
  id: number
  rating: number
  comment: string
  updatedAt: string
}

type BlockedReason = 'notLoggedIn' | 'notEnrolled' | 'owner' | 'notPublished' | null

interface ReviewsResponse {
  averageRating: number | null
  reviewCount: number
  distribution: number[]
  canReview: boolean
  reviewBlockedReason: BlockedReason
  myReview: MyReview | null
  reviews: Review[]
}

interface ReviewStats {
  averageRating: number | null
  reviewCount: number
}

const MAX_COMMENT_LENGTH = 1000

const props = defineProps<{ courseId: number }>()
const emit = defineEmits<{ (e: 'changed', stats: ReviewStats): void }>()

const route = useRoute()

const data = ref<ReviewsResponse | null>(null)
const isLoading = ref(true)
const loadError = ref('')

const formRating = ref(0)
const formComment = ref('')
const isEditing = ref(false)
const isSaving = ref(false)
const formError = ref('')
const formSuccess = ref('')

const isAdmin = (localStorage.getItem('role') || '').toLowerCase() === 'admin'

const getConfig = () => {
  const token = localStorage.getItem('token')
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {}
}

const getErrorMessage = (error: unknown, fallback: string): string => {
  if (axios.isAxiosError(error)) {
    const body = error.response?.data
    if (typeof body === 'string' && body.trim()) return body
    if (body && typeof body === 'object') {
      if (typeof body.message === 'string') return body.message
      // Błędy walidacji ASP.NET: { errors: { Pole: ["komunikat"] } }
      const firstError = body.errors ? Object.values(body.errors as Record<string, string[]>).flat()[0] : undefined
      if (typeof firstError === 'string') return firstError
    }
  }
  return fallback
}

const resetForm = () => {
  formRating.value = data.value?.myReview?.rating ?? 0
  formComment.value = data.value?.myReview?.comment ?? ''
  formError.value = ''
}

const fetchReviews = async () => {
  isLoading.value = true
  loadError.value = ''
  try {
    const response = await axios.get<ReviewsResponse>(`/api/courses/${props.courseId}/reviews`, getConfig())
    data.value = response.data
    isEditing.value = !response.data.myReview
    resetForm()
  } catch (error) {
    console.error('Błąd pobierania ocen:', error)
    loadError.value = getErrorMessage(error, 'Nie udało się pobrać ocen kursu.')
  } finally {
    isLoading.value = false
  }
}

const emitStats = () => {
  if (!data.value) return
  emit('changed', { averageRating: data.value.averageRating, reviewCount: data.value.reviewCount })
}

const saveReview = async () => {
  if (formRating.value < 1 || isSaving.value) {
    formError.value = 'Wybierz liczbę gwiazdek.'
    return
  }

  isSaving.value = true
  formError.value = ''
  formSuccess.value = ''
  try {
    const response = await axios.put(
      `/api/courses/${props.courseId}/reviews/mine`,
      { rating: formRating.value, comment: formComment.value },
      getConfig()
    )
    const successMessage: string = response.data?.message ?? 'Zapisano ocenę.'
    await fetchReviews()
    formSuccess.value = successMessage
    emitStats()
  } catch (error) {
    console.error('Błąd zapisu oceny:', error)
    formError.value = getErrorMessage(error, 'Nie udało się zapisać oceny.')
  } finally {
    isSaving.value = false
  }
}

const deleteMyReview = async () => {
  if (!confirm('Na pewno usunąć Twoją ocenę?')) return
  formSuccess.value = ''
  try {
    await axios.delete(`/api/courses/${props.courseId}/reviews/mine`, getConfig())
    await fetchReviews()
    formSuccess.value = 'Usunięto Twoją ocenę.'
    emitStats()
  } catch (error) {
    console.error('Błąd usuwania oceny:', error)
    formError.value = getErrorMessage(error, 'Nie udało się usunąć oceny.')
  }
}

// Moderacja - tylko admin
const deleteReviewAsAdmin = async (review: Review) => {
  if (!confirm(`Usunąć opinię użytkownika ${review.username}?`)) return
  try {
    await axios.delete(`/api/courses/${props.courseId}/reviews/${review.id}`, getConfig())
    await fetchReviews()
    emitStats()
  } catch (error) {
    console.error('Błąd usuwania opinii:', error)
    alert(getErrorMessage(error, 'Nie udało się usunąć opinii.'))
  }
}

const startEditing = () => {
  resetForm()
  formSuccess.value = ''
  isEditing.value = true
}

const cancelEditing = () => {
  resetForm()
  isEditing.value = false
}

const averageText = computed(() =>
  data.value?.averageRating != null
    ? data.value.averageRating.toLocaleString('pl-PL', { minimumFractionDigits: 1, maximumFractionDigits: 1 })
    : '–'
)

// Rozkład od 5★ do 1★ (tak jak w sklepach z aplikacjami)
const distributionRows = computed(() => {
  const distribution = data.value?.distribution ?? [0, 0, 0, 0, 0]
  const total = data.value?.reviewCount ?? 0
  return [5, 4, 3, 2, 1].map(stars => {
    const count = distribution[stars - 1] ?? 0
    return { stars, count, percent: total === 0 ? 0 : Math.round((count / total) * 100) }
  })
})

const blockedMessage = computed(() => {
  switch (data.value?.reviewBlockedReason) {
    case 'notLoggedIn': return 'Zaloguj się, aby ocenić ten kurs.'
    case 'notEnrolled': return 'Zapisz się na kurs, aby go ocenić.'
    case 'owner': return 'To Twój kurs - tutaj zobaczysz, jak oceniają go kursanci.'
    case 'notPublished': return 'Kurs nie jest jeszcze opublikowany, więc nie można go oceniać.'
    default: return ''
  }
})

const loginLink = computed(() => ({ path: '/login', query: { redirect: route.fullPath } }))

const formatDate = (value: string) =>
  new Date(value).toLocaleDateString('pl-PL', { day: 'numeric', month: 'long', year: 'numeric' })

// Ocena była zmieniana później niż minutę po dodaniu
const wasEdited = (review: Review) =>
  new Date(review.updatedAt).getTime() - new Date(review.createdAt).getTime() > 60_000

const initial = (username: string) => username.trim().charAt(0).toUpperCase() || '?'

watch(() => props.courseId, fetchReviews)
onMounted(fetchReviews)
</script>

<template>
  <div class="flex flex-col gap-6 text-floral-white">
    <div v-if="isLoading" class="py-6 text-center text-silver animate-pulse">Wczytywanie ocen...</div>

    <div v-else-if="loadError" class="flex flex-col items-center gap-3 py-6 text-center">
      <p class="text-spicy-paprika">{{ loadError }}</p>
      <button
        type="button"
        class="btn btn-light btn-sm"
        @click="fetchReviews"
      >Spróbuj ponownie</button>
    </div>

    <template v-else-if="data">
      <!-- Średnia + rozkład -->
      <section class="flex flex-col gap-5 rounded-2xl border border-silver/30 bg-floral-white/5 p-5 sm:flex-row sm:items-center">
        <div class="flex shrink-0 flex-col items-center gap-1 sm:w-40">
          <p class="text-5xl font-extrabold">{{ averageText }}</p>
          <StarRating :model-value="data.averageRating ?? 0" readonly size="md" />
          <p class="text-sm text-silver">
            {{ data.reviewCount > 0 ? reviewsLabel(data.reviewCount) : 'Brak ocen' }}
          </p>
        </div>

        <div class="flex flex-1 flex-col gap-1.5">
          <div v-for="row in distributionRows" :key="row.stars" class="flex items-center gap-2 text-sm">
            <span class="w-8 shrink-0 text-right text-silver">{{ row.stars }}★</span>
            <div class="h-2 flex-1 overflow-hidden rounded-full bg-floral-white/15">
              <div class="h-full rounded-full bg-amber-400 transition-all duration-500" :style="{ width: row.percent + '%' }"></div>
            </div>
            <span class="w-8 shrink-0 text-silver">{{ row.count }}</span>
          </div>
        </div>
      </section>

      <!-- Twoja ocena -->
      <section v-if="data.canReview" class="rounded-2xl border border-silver/30 p-5">
        <h3 class="mb-3 text-sm font-bold uppercase tracking-wider text-silver">Twoja ocena</h3>

        <!-- Podgląd zapisanej oceny -->
        <div v-if="data.myReview && !isEditing" class="flex flex-col gap-3">
          <StarRating :model-value="data.myReview.rating" readonly size="md" />
          <p v-if="data.myReview.comment" class="whitespace-pre-line break-words text-floral-white/90">{{ data.myReview.comment }}</p>
          <p v-else class="text-sm italic text-silver">Bez komentarza.</p>
          <div class="flex gap-3">
            <button
              type="button"
              class="btn btn-outline-dark btn-sm"
              @click="startEditing"
            >Edytuj</button>
            <button
              type="button"
              class="btn btn-ghost btn-sm text-spicy-paprika"
              @click="deleteMyReview"
            >Usuń</button>
          </div>
        </div>

        <!-- Formularz -->
        <form v-else class="flex flex-col gap-3" @submit.prevent="saveReview">
          <div class="flex items-center gap-3">
            <StarRating v-model="formRating" size="lg" />
            <span v-if="formRating" class="text-sm text-silver">{{ formRating }}/5</span>
          </div>

          <label for="review-comment" class="sr-only">Komentarz</label>
          <textarea
            id="review-comment"
            v-model="formComment"
            rows="4"
            :maxlength="MAX_COMMENT_LENGTH"
            placeholder="Napisz, co Ci się podobało, a co można poprawić (opcjonalnie)"
            class="input input-dark"
          ></textarea>
          <p class="-mt-2 text-right text-xs text-silver">{{ formComment.length }}/{{ MAX_COMMENT_LENGTH }}</p>

          <p v-if="formError" class="text-sm font-bold text-red-300" role="alert">{{ formError }}</p>

          <div class="flex justify-end gap-3">
            <button
              v-if="data.myReview"
              type="button"
              class="btn btn-outline-dark btn-sm"
              @click="cancelEditing"
            >Anuluj</button>
            <button
              type="submit"
              :disabled="formRating < 1 || isSaving"
              class="btn btn-primary btn-sm"
            >{{ isSaving ? 'Zapisywanie...' : data.myReview ? 'Zapisz zmiany' : 'Dodaj ocenę' }}</button>
          </div>
        </form>

        <p v-if="formSuccess" class="mt-3 text-sm font-bold text-green-400" role="status">{{ formSuccess }}</p>
      </section>

      <p v-else-if="blockedMessage" class="rounded-xl border border-silver/30 p-4 text-center text-sm text-silver">
        {{ blockedMessage }}
        <router-link
          v-if="data.reviewBlockedReason === 'notLoggedIn'"
          :to="loginLink"
          class="ml-1 font-semibold text-spicy-paprika hover:underline"
        >Zaloguj się</router-link>
      </p>

      <!-- Lista opinii -->
      <section class="flex flex-col gap-3">
        <h3 class="text-sm font-bold uppercase tracking-wider text-silver">Opinie kursantów</h3>

        <p v-if="data.reviews.length === 0" class="text-sm text-silver">
          Nikt jeszcze nie ocenił tego kursu.{{ data.canReview ? ' Bądź pierwszy!' : '' }}
        </p>

        <article
          v-for="review in data.reviews"
          :key="review.id"
          class="rounded-xl border p-4"
          :class="review.isMine ? 'border-spicy-paprika/60 bg-spicy-paprika/5' : 'border-silver/30'"
        >
          <header class="flex items-start gap-3">
            <img
              v-if="review.avatarUrl"
              :src="review.avatarUrl"
              :alt="`Avatar ${review.username}`"
              class="h-9 w-9 shrink-0 rounded-full object-cover"
            />
            <span
              v-else
              class="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-floral-white/15 font-bold"
              aria-hidden="true"
            >{{ initial(review.username) }}</span>

            <div class="min-w-0 flex-1">
              <p class="flex flex-wrap items-center gap-2 font-semibold">
                <span class="truncate">{{ review.username }}</span>
                <span v-if="review.isMine" class="rounded bg-spicy-paprika px-1.5 py-0.5 text-[10px] uppercase tracking-wider text-white">Ty</span>
              </p>
              <div class="mt-0.5 flex flex-wrap items-center gap-2 text-xs text-silver">
                <StarRating :model-value="review.rating" readonly size="sm" />
                <span>{{ formatDate(review.createdAt) }}</span>
                <span v-if="wasEdited(review)">(edytowano)</span>
              </div>
            </div>

            <button
              v-if="isAdmin && !review.isMine"
              type="button"
              class="shrink-0 rounded px-2 py-1 text-xs font-semibold text-spicy-paprika hover:bg-spicy-paprika/10"
              title="Usuń opinię (moderacja)"
              @click="deleteReviewAsAdmin(review)"
            >Usuń</button>
          </header>

          <p v-if="review.comment" class="mt-3 whitespace-pre-line break-words text-sm text-floral-white/90">{{ review.comment }}</p>
        </article>
      </section>
    </template>
  </div>
</template>
