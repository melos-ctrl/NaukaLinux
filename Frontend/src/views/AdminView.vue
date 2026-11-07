<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const router = useRouter()
const route = useRoute()
type AdminTab = 'courses' | 'users' | 'results' | 'feedbacks' | 'mentorApplications'
const isAdminTab = (value: unknown): value is AdminTab =>
  ['courses', 'users', 'results', 'feedbacks', 'mentorApplications'].includes(String(value))
const activeTab = ref<AdminTab>(isAdminTab(route.query.tab) ? route.query.tab : 'courses')
const API_URL = '/api/admin'

const getHeaders = () => ({
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${localStorage.getItem('token')}`
})

// stan danych
const courses = ref<any[]>([])
const users = ref<any[]>([])
const results = ref<any[]>([])
const feedbacks = ref<any[]>([])
const mentorApplications = ref<any[]>([])
const mentorApplicationsError = ref('')
const reviewingApplicationId = ref<number | null>(null)
const reviewError = ref('')

// pobieranie danych
const fetchAll = async () => {
  mentorApplicationsError.value = ''
  const [cRes, uRes, rRes, fRes, mRes] = await Promise.all([
    fetch(`${API_URL}/courses`, { headers: getHeaders() }),
    fetch(`${API_URL}/users`, { headers: getHeaders() }),
    fetch(`${API_URL}/results`, { headers: getHeaders() }),
    fetch(`${API_URL}/feedbacks`, { headers: getHeaders() }),
    fetch(`${API_URL}/mentor-applications`, { headers: getHeaders() }).catch((error: unknown) => {
      console.error('Nie można połączyć się z endpointem zgłoszeń mentorskich:', error)
      mentorApplicationsError.value = 'Nie można połączyć się z API zgłoszeń mentorów. Upewnij się, że backend działa.'
      return null
    })
  ])
  if (cRes.ok) courses.value = await cRes.json()
  if (uRes.ok) users.value = await uRes.json()
  if (rRes.ok) results.value = await rRes.json()
  if (fRes.ok) feedbacks.value = await fRes.json()
  if (mRes?.ok) {
    mentorApplications.value = await mRes.json()
    mentorApplicationsError.value = ''
  } else if (mRes) {
    mentorApplications.value = []
    mentorApplicationsError.value = `Nie udało się pobrać zgłoszeń (HTTP ${mRes.status}).`
    console.error('Pobranie zgłoszeń mentorskich nie powiodło się:', mRes.status, await mRes.text())
  }
}

const editCourse = (courseId: number) => {
  router.push(`/course-creator/${courseId}`)
}

const deleteCourse = async (id: number) => {
  if (!confirm('Usunąć ten kurs?')) return
  await fetch(`${API_URL}/courses/${id}`, { method: 'DELETE', headers: getHeaders() })
  await fetchAll()
}

// --- AKCJE UŻYTKOWNIKÓW ---
const deleteUser = async (id: number) => {
  if (!confirm('Usunąć użytkownika?')) return
  await fetch(`${API_URL}/users/${id}`, { method: 'DELETE', headers: getHeaders() })
  await fetchAll()
}

const changeRole = async (user: any, event: Event) => {
  const newRole = (event.target as HTMLSelectElement).value
  await fetch(`${API_URL}/users/${user.id}/role`, {
    method: 'PUT',
    headers: getHeaders(),
    body: JSON.stringify(newRole)
  })
  await fetchAll()
}

const reviewMentorApplication = async (applicationId: number, status: 'Approved' | 'Rejected') => {
  reviewingApplicationId.value = applicationId
  reviewError.value = ''

  try {
    const response = await fetch(`${API_URL}/mentor-applications/${applicationId}/review`, {
      method: 'PUT',
      headers: getHeaders(),
      body: JSON.stringify({ status })
    })

    if (!response.ok) {
      reviewError.value = await response.text() || 'Nie udało się rozpatrzyć zgłoszenia.'
      return
    }

    await fetchAll()
  } catch (error) {
    console.error('Nie udało się rozpatrzyć zgłoszenia:', error)
    reviewError.value = 'Nie udało się połączyć z serwerem.'
  } finally {
    reviewingApplicationId.value = null
  }
}

watch(() => route.query.tab, (tab) => {
  if (isAdminTab(tab)) activeTab.value = tab
})

onMounted(fetchAll)
</script>

<template>
  <div class="-m-4 flex min-h-0 flex-1 flex-col gap-4 md:h-full md:flex-row md:gap-4">
    <aside
      class="m-2 flex w-auto shrink-0 flex-col rounded-2xl border border-silver/20 bg-charcoal-brown text-floral-white shadow-lg md:w-64"
    >
      <section class="flex flex-col items-center border-b border-silver/20 p-6 text-center">
        <div class="flex h-20 w-20 items-center justify-center rounded-full border-4 border-spicy-paprika bg-carbon-black text-3xl font-extrabold shadow-lg" aria-hidden="true">
          A
        </div>
        <h2 class="mt-4 font-bold tracking-wide">Administracja</h2>
        <p class="mt-1 text-sm text-silver">Panel administratora</p>
      </section>

      <nav class="flex flex-row gap-1 overflow-x-auto p-3 md:flex-col" aria-label="Nawigacja panelu administratora">
        <button
          type="button"
          class="shrink-0 rounded-lg px-4 py-3 text-left font-medium transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          :class="activeTab === 'courses' ? 'bg-spicy-paprika text-floral-white' : 'text-silver'"
          @click="activeTab = 'courses'"
        >
          Kursy na platformie
        </button>
        <router-link
          to="/course-creator"
          class="rounded-lg px-4 py-3 font-medium text-silver transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          active-class="bg-spicy-paprika text-floral-white"
        >
          + Kreator Kursów
        </router-link>
        <button
          type="button"
          class="shrink-0 rounded-lg px-4 py-3 text-left font-medium transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          :class="activeTab === 'users' ? 'bg-spicy-paprika text-floral-white' : 'text-silver'"
          @click="activeTab = 'users'"
        >
          Użytkownicy
        </button>
        <button
          type="button"
          class="shrink-0 rounded-lg px-4 py-3 text-left font-medium transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          :class="activeTab === 'results' ? 'bg-spicy-paprika text-floral-white' : 'text-silver'"
          @click="activeTab = 'results'"
        >
          Wyniki uczestników
        </button>
        <button
          type="button"
          class="shrink-0 rounded-lg px-4 py-3 text-left font-medium transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          :class="activeTab === 'feedbacks' ? 'bg-spicy-paprika text-floral-white' : 'text-silver'"
          @click="activeTab = 'feedbacks'"
        >
          Zgłoszenia i opinie
        </button>
        <button
          type="button"
          class="shrink-0 rounded-lg px-4 py-3 text-left font-medium transition-colors hover:bg-spicy-paprika hover:text-floral-white"
          :class="activeTab === 'mentorApplications' ? 'bg-spicy-paprika text-floral-white' : 'text-silver'"
          @click="activeTab = 'mentorApplications'"
        >
          Kandydaci na mentorów ({{ mentorApplications.length }})
        </button>
      </nav>
    </aside>

    <main class="min-h-0 min-w-0 flex-1 space-y-8 overflow-y-auto p-5 md:p-8 lg:p-10">
      <header>
        <p class="text-sm font-semibold uppercase tracking-wider text-spicy-paprika">
          Strefa zarządzania
        </p>
        <h1 class="mt-1 text-3xl font-bold text-charcoal-brown">Panel administratora</h1>
        <p class="mt-2 text-charcoal-brown/70">
          Zarządzaj kursami, zadaniami, użytkownikami i wynikami.
        </p>
      </header>

      <!-- kursy -->
      <section v-if="activeTab === 'courses'" class="space-y-6">
        <div class="flex justify-between items-center">
          <div>
            <p class="text-sm font-medium text-silver">Katalog</p>
            <h2 class="text-xl font-bold text-charcoal-brown">Wszystkie kursy</h2>
          </div>
          <router-link
            to="/course-creator"
            class="rounded-lg bg-spicy-paprika px-5 py-2.5 font-semibold text-white transition-colors hover:opacity-90"
          >
            + Dodaj nowy kurs w kreatorze
          </router-link>
        </div>

        <div v-if="courses.length" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          <article v-for="course in courses" :key="course.id" class="flex min-h-48 flex-col rounded-xl border border-silver/50 bg-white p-5 shadow-sm">
            <span class="mb-3 w-fit rounded-full bg-spicy-paprika/10 px-3 py-1 text-xs font-semibold text-spicy-paprika">
              {{ course.isPublished ? 'Opublikowany' : 'Szkic' }}
            </span>
            <h3 class="text-lg font-bold text-charcoal-brown">{{ course.title }}</h3>
            <p class="mt-2 flex-1 text-sm leading-relaxed text-charcoal-brown/70">{{ course.description }}</p>
            <span class="mt-4 text-xs font-medium text-sage">Lekcji w kursie: {{ course.lessons?.length || 0 }}</span>
            <div class="mt-4 flex flex-wrap gap-2">
              <button type="button" class="rounded-lg bg-charcoal-brown px-3 py-2 text-sm font-medium text-white transition-colors hover:bg-spicy-paprika" @click="editCourse(course.id)">
                Edytuj kurs i zadania
              </button>
              <button type="button" class="rounded-lg border border-silver/70 px-3 py-2 text-sm font-medium text-charcoal-brown transition-colors hover:border-red-500 hover:text-red-700" @click="deleteCourse(course.id)">
                Usuń
              </button>
            </div>
          </article>
        </div>
        <p v-else class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm">
          Nie ma jeszcze żadnych kursów.
        </p>
      </section>

      <!-- użytkownicy -->
      <section v-if="activeTab === 'users'" class="space-y-4">
        <div>
          <p class="text-sm font-medium text-silver">Konta</p>
          <h2 class="text-xl font-bold text-charcoal-brown">Użytkownicy</h2>
        </div>
        <div v-if="users.length" class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
          <table class="w-full min-w-[600px] border-collapse text-left">
            <thead class="bg-floral-white text-sm text-charcoal-brown/70">
              <tr>
                <th class="px-5 py-4 font-semibold">ID</th>
                <th class="px-5 py-4 font-semibold">Nazwa</th>
                <th class="px-5 py-4 font-semibold">Email</th>
                <th class="px-5 py-4 font-semibold">Rola</th>
                <th class="px-5 py-4 font-semibold">Akcje</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-silver/40 text-charcoal-brown">
              <tr v-for="user in users" :key="user.id">
                <td class="px-5 py-4">{{ user.id }}</td>
                <td class="px-5 py-4 font-medium">{{ user.username }}</td>
                <td class="px-5 py-4">{{ user.email }}</td>
                <td class="px-5 py-4">
                  <span class="rounded-full bg-spicy-paprika/10 px-3 py-1 text-xs font-semibold text-spicy-paprika">{{ user.role }}</span>
                </td>
                <td class="px-5 py-4">
                  <div class="flex flex-wrap gap-2">
                    <select
                      :value="user.role"
                      aria-label="Zmień rolę użytkownika"
                      class="rounded-lg border border-silver/70 bg-floral-white px-3 py-2 text-sm font-medium text-charcoal-brown focus-visible:outline-2 focus-visible:outline-spicy-paprika"
                      @change="changeRole(user, $event)"
                    >
                      <option value="User">Użytkownik</option>
                      <option value="Mentor">Mentor</option>
                      <option value="Admin">Administrator</option>
                    </select>
                    <button type="button" class="rounded-lg border border-silver/70 px-3 py-2 text-sm font-medium text-charcoal-brown transition-colors hover:border-red-500 hover:text-red-700" @click="deleteUser(user.id)">Usuń</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm">
          Nie znaleziono użytkowników.
        </p>
      </section>

      <!-- ZAKŁADKA: WYNIKI UCZESTNIKÓW -->
      <section v-if="activeTab === 'results'" class="space-y-4">
        <div>
          <p class="text-sm font-medium text-silver">Postępy</p>
          <h2 class="text-xl font-bold text-charcoal-brown">Wyniki uczestników</h2>
        </div>
        <div v-if="results.length" class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
          <table class="w-full min-w-[700px] border-collapse text-left">
            <thead class="bg-floral-white text-sm text-charcoal-brown/70">
              <tr>
                <th class="px-5 py-4 font-semibold">Uczestnik</th>
                <th class="px-5 py-4 font-semibold">Kurs</th>
                <th class="px-5 py-4 font-semibold">Ukończony etap (Lekcja)</th>
                <th class="px-5 py-4 font-semibold">Wynik</th>
                <th class="px-5 py-4 font-semibold">Ostatnia aktywność</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-silver/40 text-charcoal-brown">
              <tr v-for="result in results" :key="result.id">
                <td class="px-5 py-4">{{ result.username }} ({{ result.email }})</td>
                <td class="px-5 py-4 font-medium">{{ result.courseTitle }}</td>
                <td class="px-5 py-4">Lekcja {{ result.lastCompletedLessonId }}</td>
                <td class="px-5 py-4 font-bold text-sage">{{ result.score }} pkt</td>
                <td class="px-5 py-4">{{ new Date(result.updatedAt).toLocaleString() }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm">
          Nie ma jeszcze wyników do wyświetlenia.
        </p>
      </section>

      <!-- zgłaszane błędy pojawią się tutaj -->
      <section v-if="activeTab === 'feedbacks'" class="space-y-4">
        <div>
          <p class="text-sm font-medium text-silver">Kontakt od kursantów</p>
          <h2 class="text-xl font-bold text-charcoal-brown">Zgłoszone błędy i opinie</h2>
        </div>
        <div v-if="feedbacks.length" class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
          <table class="w-full min-w-[700px] border-collapse text-left">
            <thead class="bg-floral-white text-sm text-charcoal-brown/70">
              <tr>
                <th class="px-5 py-4 font-semibold">Użytkownik</th>
                <th class="px-5 py-4 font-semibold">Typ</th>
                <th class="px-5 py-4 font-semibold">Treść wiadomości</th>
                <th class="px-5 py-4 font-semibold">Data</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-silver/40 text-charcoal-brown">
              <tr v-for="fb in feedbacks" :key="fb.id">
                <td class="px-5 py-4 font-medium">{{ fb.username }}</td>
                <td class="px-5 py-4">
                  <span 
                    class="rounded-full px-3 py-1 text-xs font-semibold"
                    :class="fb.lessonBlockId ? 'bg-red-100 text-red-700' : 'bg-blue-100 text-blue-700'"
                  >
                    {{ fb.type }}
                  </span>
                </td>
                <td class="px-5 py-4">{{ fb.message }}</td>
                <td class="px-5 py-4 text-sm text-charcoal-brown/70">{{ new Date(fb.createdAt).toLocaleString() }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm">
          Brak zgłoszeń i opinii.
        </p>
      </section>

      <section v-if="activeTab === 'mentorApplications'" class="space-y-4">
        <div>
          <p class="text-sm font-medium text-silver">Zgłoszenia</p>
          <h2 class="text-xl font-bold text-charcoal-brown">Kandydaci na mentorów</h2>
        </div>
        <p v-if="reviewError" role="alert" class="text-sm font-semibold text-red-700">{{ reviewError }}</p>
        <p v-if="mentorApplicationsError" role="alert" class="rounded-lg border border-red-300 bg-red-50 p-4 text-sm font-semibold text-red-800">
          {{ mentorApplicationsError }} Sprawdź logowanie administratora i odpowiedź żądania <code>/api/admin/mentor-applications</code> w Network.
        </p>
        <div v-else-if="mentorApplications.length" class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
          <table class="w-full min-w-[900px] border-collapse text-left">
            <thead class="bg-floral-white text-sm text-charcoal-brown/70">
              <tr>
                <th class="px-5 py-4 font-semibold">Kandydat</th>
                <th class="px-5 py-4 font-semibold">Doświadczenie</th>
                <th class="px-5 py-4 font-semibold">Portfolio</th>
                <th class="px-5 py-4 font-semibold">Status</th>
                <th class="px-5 py-4 font-semibold">Data zgłoszenia</th>
                <th class="px-5 py-4 font-semibold">Decyzja</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-silver/40 text-charcoal-brown">
              <tr v-for="application in mentorApplications" :key="application.id" class="align-top">
                <td class="px-5 py-4">
                  <p class="font-semibold">{{ application.firstName }} {{ application.lastName }}</p>
                  <p class="mt-1 text-sm">{{ application.username }} · {{ application.email }}</p>
                </td>
                <td class="max-w-lg whitespace-pre-wrap px-5 py-4 text-sm">{{ application.experience }}</td>
                <td class="px-5 py-4">
                  <a v-if="application.portfolioUrl" :href="application.portfolioUrl" target="_blank" rel="noopener noreferrer" class="font-medium text-spicy-paprika underline">Otwórz portfolio</a>
                  <span v-else class="text-silver">Brak</span>
                </td>
                <td class="px-5 py-4">
                  <span class="rounded-full bg-sky-blue/10 px-3 py-1 text-xs font-semibold text-sky-blue">{{ application.status === 'Pending' ? 'Oczekuje' : application.status }}</span>
                </td>
                <td class="whitespace-nowrap px-5 py-4 text-sm">{{ new Date(application.submittedAt).toLocaleString() }}</td>
                <td class="px-5 py-4">
                  <div v-if="application.status === 'Pending'" class="flex gap-2">
                    <button
                      type="button"
                      :disabled="reviewingApplicationId === application.id"
                      class="rounded-md bg-green-700 px-3 py-2 text-sm font-semibold text-white hover:bg-green-800 disabled:opacity-50"
                      @click="reviewMentorApplication(application.id, 'Approved')"
                    >Zatwierdź</button>
                    <button
                      type="button"
                      :disabled="reviewingApplicationId === application.id"
                      class="rounded-md border border-red-700 px-3 py-2 text-sm font-semibold text-red-700 hover:bg-red-50 disabled:opacity-50"
                      @click="reviewMentorApplication(application.id, 'Rejected')"
                    >Odrzuć</button>
                  </div>
                  <span v-else class="text-sm text-silver">Rozpatrzono</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <p v-else-if="!mentorApplicationsError" class="rounded-xl border border-silver/50 bg-white p-6 text-charcoal-brown/70 shadow-sm">
          Nie ma jeszcze zgłoszeń na mentora.
        </p>
      </section>
    </main>
  </div>
</template>