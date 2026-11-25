<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'
import PageHeader from '@/components/PageHeader.vue'

interface MentorApplication {
  firstName: string
  lastName: string
  experience: string
  portfolioUrl: string | null
  status: string
  submittedAt: string
  reviewedAt: string | null
  canResubmitAt: string | null
  canResubmit: boolean
}

const router = useRouter()
const firstName = ref('')
const lastName = ref('')
const experience = ref('')
const portfolioUrl = ref('')
const application = ref<MentorApplication | null>(null)
const isLoading = ref(true)
const isSubmitting = ref(false)
const errorMessage = ref('')

const getApiErrorMessage = (error: unknown) => {
  if (!axios.isAxiosError(error)) {
    return 'Nie udało się wysłać zgłoszenia. Spróbuj ponownie.'
  }

  const data = error.response?.data
  if (typeof data === 'string') return data
  if (typeof data?.detail === 'string') return data.detail
  if (typeof data?.title === 'string') return data.title

  const validationErrors = data?.errors as Record<string, string[]> | undefined
  if (validationErrors) {
    const firstError = Object.values(validationErrors).flat()[0]
    if (firstError) return firstError
  }

  return 'Nie udało się wysłać zgłoszenia. Sprawdź poprawność danych i spróbuj ponownie.'
}

const getConfig = () => ({
  headers: { Authorization: `Bearer ${localStorage.getItem('token')}` },
})

const loadApplication = async () => {
  try {
    const response = await axios.get('/api/mentor-applications/me', getConfig())
    if (response.status !== 204) {
      application.value = response.data
      firstName.value = response.data.firstName
      lastName.value = response.data.lastName
      experience.value = response.data.experience
      portfolioUrl.value = response.data.portfolioUrl ?? ''
    }
  } catch (error) {
    console.error('Nie udało się pobrać zgłoszenia:', error)
    errorMessage.value = 'Nie udało się wczytać formularza. Odśwież stronę i spróbuj ponownie.'
  } finally {
    isLoading.value = false
  }
}

const submitApplication = async () => {
  isSubmitting.value = true
  errorMessage.value = ''

  try {
    const response = await axios.post('/api/mentor-applications', {
      firstName: firstName.value,
      lastName: lastName.value,
      experience: experience.value,
      portfolioUrl: portfolioUrl.value || null,
    }, getConfig())
    application.value = response.data
  } catch (error) {
    console.error('Nie udało się wysłać zgłoszenia:', error)
    errorMessage.value = getApiErrorMessage(error)
  } finally {
    isSubmitting.value = false
  }
}

onMounted(loadApplication)
</script>

<template>
  <div class="page-narrow">
    <button type="button" @click="router.back()" class="btn btn-ghost -ml-3">← Wróć</button>

    <PageHeader
      eyebrow="Zgłoszenie"
      title="Zostań mentorem"
      lead="Opowiedz o swoim doświadczeniu. Zgłoszenie zostanie przekazane do rozpatrzenia."
    />

    <div v-if="isLoading" class="py-10 text-muted animate-pulse" role="status">Wczytywanie formularza...</div>

    <section v-else-if="application && application.status !== 'Rejected'" role="status" aria-live="polite" class="card space-y-2">
      <span class="badge badge-success">Wysłano</span>
      <h2 class="section-title">Zgłoszenie zostało wysłane</h2>
      <p>Dziękujemy za zgłoszenie. Oczekuj na jego rozpatrzenie.</p>
      <p>Status: <span class="badge badge-info">{{ application.status === 'Pending' ? 'Oczekuje na rozpatrzenie' : application.status }}</span></p>
      <p class="text-sm text-muted">Wysłano: {{ new Date(application.submittedAt).toLocaleString() }}</p>
    </section>

    <section v-else-if="application?.status === 'Rejected'" role="status" class="card space-y-2">
      <span class="badge badge-danger">Odrzucone</span>
      <h2 class="section-title">Poprzednie zgłoszenie zostało odrzucone</h2>
      <p v-if="!application.canResubmit && application.canResubmitAt">
        Ponowne zgłoszenie będzie możliwe od {{ new Date(application.canResubmitAt).toLocaleString() }}.
      </p>
      <p v-else>Możesz teraz wysłać nowe zgłoszenie.</p>
    </section>

    <form
      v-if="!isLoading && (!application || (application.status === 'Rejected' && application.canResubmit))"
      @submit.prevent="submitApplication"
      class="card space-y-6"
    >
      <div class="grid gap-5 sm:grid-cols-2">
        <div>
          <label for="firstName" class="label">Imię</label>
          <input id="firstName" v-model.trim="firstName" required maxlength="80" autocomplete="given-name" class="input" />
        </div>
        <div>
          <label for="lastName" class="label">Nazwisko</label>
          <input id="lastName" v-model.trim="lastName" required maxlength="80" autocomplete="family-name" class="input" />
        </div>
      </div>

      <div>
        <label for="experience" class="label">Opis doświadczenia</label>
        <textarea
          id="experience"
          v-model.trim="experience"
          required
          minlength="30"
          maxlength="4000"
          rows="7"
          placeholder="Opisz swoje doświadczenie, obszary wiedzy i tematy, których możesz uczyć."
          class="input resize-y"
        ></textarea>
        <span class="mt-1 flex justify-between text-xs" :class="experience.trim().length < 30 ? 'text-paprika-600' : 'text-muted'">
          <span>Minimum 30 znaków</span>
          <span>{{ experience.trim().length }} / 4000</span>
        </span>
      </div>

      <div>
        <label for="portfolioUrl" class="label">Link do portfolio <span class="font-normal text-muted">(opcjonalnie)</span></label>
        <input id="portfolioUrl" v-model.trim="portfolioUrl" type="url" maxlength="2048" placeholder="https://..." class="input" />
      </div>

      <p v-if="errorMessage" role="alert" class="alert alert-error">{{ errorMessage }}</p>

      <button type="submit" :disabled="isSubmitting" class="btn btn-primary">
        {{ isSubmitting ? 'Wysyłanie...' : 'Wyślij zgłoszenie' }}
      </button>
    </form>
  </div>
</template>
