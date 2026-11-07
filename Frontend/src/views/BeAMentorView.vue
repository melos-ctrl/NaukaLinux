<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import axios from 'axios'

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
  <div class="min-h-screen bg-floral-white text-charcoal-brown px-5 py-12">
    <main class="mx-auto w-full max-w-3xl">
      <button @click="router.back()" class="mb-8 text-sm font-semibold text-charcoal-brown hover:text-spicy-paprika">← Wróć</button>
      <header class="mb-8 border-b border-silver pb-6">
        <p class="text-sm font-bold uppercase text-spicy-paprika">Zgłoszenie</p>
        <h1 class="mt-2 text-3xl font-bold">Zostań mentorem</h1>
        <p class="mt-3 max-w-2xl text-charcoal-brown/70">Opowiedz o swoim doświadczeniu. Zgłoszenie zostanie przekazane do rozpatrzenia.</p>
      </header>

      <div v-if="isLoading" class="py-10 text-silver">Wczytywanie formularza...</div>
      <section v-else-if="application && application.status !== 'Rejected'" role="status" aria-live="polite" class="border-l-4 border-green-600 bg-white p-6">
        <h2 class="text-xl font-bold text-green-700">Zgłoszenie zostało wysłane</h2>
        <p class="mt-2">Dziękujemy za zgłoszenie. Oczekuj na jego rozpatrzenie.</p>
        <p class="mt-2">Status: <strong>{{ application.status === 'Pending' ? 'Oczekuje na rozpatrzenie' : application.status }}</strong></p>
        <p class="mt-1 text-sm text-charcoal-brown/70">Wysłano: {{ new Date(application.submittedAt).toLocaleString() }}</p>
      </section>
      <section v-else-if="application?.status === 'Rejected'" role="status" class="mb-6 border-l-4 border-spicy-paprika bg-white p-6">
        <h2 class="text-xl font-bold">Poprzednie zgłoszenie zostało odrzucone</h2>
        <p v-if="!application.canResubmit && application.canResubmitAt" class="mt-2">
          Ponowne zgłoszenie będzie możliwe od {{ new Date(application.canResubmitAt).toLocaleString() }}.
        </p>
        <p v-else class="mt-2">Możesz teraz wysłać nowe zgłoszenie.</p>
      </section>
      <form v-if="!isLoading && (!application || (application.status === 'Rejected' && application.canResubmit))" @submit.prevent="submitApplication" class="space-y-6">
        <div class="grid gap-5 sm:grid-cols-2">
          <label for="firstName" class="block text-sm font-semibold">Imię
            <input id="firstName" v-model.trim="firstName" required maxlength="80" autocomplete="given-name" class="mt-2 w-full rounded border border-silver bg-white px-4 py-3 font-normal focus:border-spicy-paprika focus:outline-none" />
          </label>
          <label for="lastName" class="block text-sm font-semibold">Nazwisko
            <input id="lastName" v-model.trim="lastName" required maxlength="80" autocomplete="family-name" class="mt-2 w-full rounded border border-silver bg-white px-4 py-3 font-normal focus:border-spicy-paprika focus:outline-none" />
          </label>
        </div>

        <label for="experience" class="block text-sm font-semibold">Opis doświadczenia
          <textarea id="experience" v-model.trim="experience" required minlength="30" maxlength="4000" rows="7" placeholder="Opisz swoje doświadczenie, obszary wiedzy i tematy, których możesz uczyć." class="mt-2 w-full resize-y rounded border border-silver bg-white px-4 py-3 font-normal focus:border-spicy-paprika focus:outline-none"></textarea>
          <span class="mt-1 flex justify-between text-xs font-normal" :class="experience.trim().length < 30 ? 'text-spicy-paprika' : 'text-charcoal-brown/60'">
            <span>Minimum 30 znaków</span>
            <span>{{ experience.trim().length }} / 4000</span>
          </span>
        </label>

        <label for="portfolioUrl" class="block text-sm font-semibold">Link do portfolio <span class="font-normal text-charcoal-brown/60">(opcjonalnie)</span>
          <input id="portfolioUrl" v-model.trim="portfolioUrl" type="url" maxlength="2048" placeholder="https://..." class="mt-2 w-full rounded border border-silver bg-white px-4 py-3 font-normal focus:border-spicy-paprika focus:outline-none" />
        </label>

        <p v-if="errorMessage" role="alert" class="text-sm font-semibold text-spicy-paprika">{{ errorMessage }}</p>
        <button type="submit" :disabled="isSubmitting" class="bg-spicy-paprika px-6 py-3 font-semibold text-white transition-colors hover:bg-charcoal-brown disabled:cursor-wait disabled:opacity-50">
          {{ isSubmitting ? 'Wysyłanie...' : 'Wyślij zgłoszenie' }}
        </button>
      </form>
    </main>
  </div>
</template>
