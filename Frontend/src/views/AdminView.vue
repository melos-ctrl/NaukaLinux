<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import SidebarLayout from '@/components/SidebarLayout.vue'
import PageHeader from '@/components/PageHeader.vue'

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
const usersError = ref('')

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
  usersError.value = ''
  const response = await fetch(`${API_URL}/users/${id}`, { method: 'DELETE', headers: getHeaders() })
  if (!response.ok) {
    usersError.value = (await response.text()) || 'Nie udało się usunąć użytkownika.'
  }
  await fetchAll()
}

const changeRole = async (user: any, event: Event) => {
  const newRole = (event.target as HTMLSelectElement).value
  usersError.value = ''
  const response = await fetch(`${API_URL}/users/${user.id}/role`, {
    method: 'PUT',
    headers: getHeaders(),
    body: JSON.stringify(newRole)
  })
  if (!response.ok) {
    usersError.value = (await response.text()) || 'Nie udało się zmienić roli.'
  }
  // odświeżamy zawsze - przy błędzie select wróci do aktualnej roli
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
  <SidebarLayout>
    <template #profile>
      <div class="flex h-20 w-20 items-center justify-center rounded-full border-4 border-spicy-paprika bg-carbon-black font-mono text-3xl font-bold shadow-lg" aria-hidden="true">
        A
      </div>
      <h2 class="mt-2 font-bold">Administracja</h2>
      <p class="text-sm text-silver">Panel administratora</p>
    </template>

    <template #nav>
      <button
        type="button"
        class="side-link shrink-0"
        :class="{ 'side-link-active': activeTab === 'courses' }"
        @click="activeTab = 'courses'"
      >
        Kursy na platformie
      </button>
      <router-link
        to="/course-creator"
        class="side-link shrink-0"
        active-class="side-link-active"
      >
        + Kreator kursów
      </router-link>
      <button
        type="button"
        class="side-link shrink-0"
        :class="{ 'side-link-active': activeTab === 'users' }"
        @click="activeTab = 'users'"
      >
        Użytkownicy
      </button>
      <button
        type="button"
        class="side-link shrink-0"
        :class="{ 'side-link-active': activeTab === 'results' }"
        @click="activeTab = 'results'"
      >
        Wyniki uczestników
      </button>
      <button
        type="button"
        class="side-link shrink-0"
        :class="{ 'side-link-active': activeTab === 'feedbacks' }"
        @click="activeTab = 'feedbacks'"
      >
        Zgłoszenia i opinie
      </button>
      <button
        type="button"
        class="side-link shrink-0"
        :class="{ 'side-link-active': activeTab === 'mentorApplications' }"
        @click="activeTab = 'mentorApplications'"
      >
        Kandydaci na mentorów ({{ mentorApplications.length }})
      </button>
    </template>

    <PageHeader
      eyebrow="Strefa zarządzania"
      title="Panel administratora"
      lead="Zarządzaj kursami, zadaniami, użytkownikami i wynikami."
    />

    <!-- kursy -->
    <section v-if="activeTab === 'courses'" class="space-y-6">
      <div class="flex flex-wrap items-end justify-between gap-4">
        <div>
          <p class="text-sm text-muted">Katalog</p>
          <h2 class="section-title">Wszystkie kursy</h2>
        </div>
        <router-link to="/course-creator" class="btn btn-primary">
          + Dodaj nowy kurs w kreatorze
        </router-link>
      </div>

      <div v-if="courses.length" class="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <article v-for="course in courses" :key="course.id" class="card flex min-h-48 flex-col md:p-5">
          <span class="badge mb-3" :class="course.isPublished ? 'badge-success' : 'badge-warning'">
            {{ course.isPublished ? 'Opublikowany' : 'Szkic' }}
          </span>
          <h3 class="text-lg font-bold">{{ course.title }}</h3>
          <p class="mt-2 flex-1 text-sm leading-relaxed text-muted">{{ course.description }}</p>
          <span class="mt-4 text-xs font-medium text-muted">Lekcji w kursie: {{ course.lessonCount ?? 0 }}</span>
          <div class="mt-4 flex flex-wrap gap-2">
            <button type="button" class="btn btn-dark btn-sm" @click="editCourse(course.id)">
              Edytuj kurs i zadania
            </button>
            <button type="button" class="btn btn-danger btn-sm" @click="deleteCourse(course.id)">
              Usuń
            </button>
          </div>
        </article>
      </div>
      <p v-else class="empty-state">
        Nie ma jeszcze żadnych kursów.
      </p>
    </section>

    <!-- użytkownicy -->
    <section v-if="activeTab === 'users'" class="space-y-4">
      <div>
        <p class="text-sm text-muted">Konta</p>
        <h2 class="section-title">Użytkownicy</h2>
      </div>
      <p v-if="usersError" role="alert" class="alert alert-error">{{ usersError }}</p>
      <div v-if="users.length" class="table-wrap">
        <table class="table min-w-[600px]">
          <thead>
            <tr>
              <th>ID</th>
              <th>Nazwa</th>
              <th>Email</th>
              <th>Rola</th>
              <th>Akcje</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="user in users" :key="user.id">
              <td>{{ user.id }}</td>
              <td class="font-medium">{{ user.username }}</td>
              <td>{{ user.email }}</td>
              <td>
                <span class="badge badge-primary">{{ user.role }}</span>
              </td>
              <td>
                <div class="flex flex-wrap gap-2">
                  <select
                    :value="user.role"
                    aria-label="Zmień rolę użytkownika"
                    class="input h-9 w-auto text-sm"
                    @change="changeRole(user, $event)"
                  >
                    <option value="User">Użytkownik</option>
                    <option value="Mentor">Mentor</option>
                    <option value="Admin">Administrator</option>
                  </select>
                  <button type="button" class="btn btn-danger btn-sm" @click="deleteUser(user.id)">Usuń</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-else class="empty-state">
        Nie znaleziono użytkowników.
      </p>
    </section>

    <!-- ZAKŁADKA: WYNIKI UCZESTNIKÓW -->
    <section v-if="activeTab === 'results'" class="space-y-4">
      <div>
        <p class="text-sm text-muted">Postępy</p>
        <h2 class="section-title">Wyniki uczestników</h2>
      </div>
      <div v-if="results.length" class="table-wrap">
        <table class="table min-w-[700px]">
          <thead>
            <tr>
              <th>Uczestnik</th>
              <th>Kurs</th>
              <th>Ukończona lekcja</th>
              <th>Wynik</th>
              <th>Ostatnia aktywność</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="result in results" :key="result.id">
              <td>{{ result.username }} ({{ result.email }})</td>
              <td class="font-medium">{{ result.courseTitle }}</td>
              <td>
                <template v-if="result.lastCompletedLessonId > 0">
                  <div>{{ result.completedLessonTitle || `Etap ${result.lastCompletedLessonId}` }}</div>
                  <div v-if="result.completedModuleTitle" class="mt-1 text-xs text-muted">{{ result.completedModuleTitle }}</div>
                </template>
                <span v-else class="text-muted">Nie rozpoczęto</span>
              </td>
              <td class="font-bold text-green-700">{{ result.score }} pkt</td>
              <td class="text-sm text-muted">{{ new Date(result.updatedAt).toLocaleString() }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-else class="empty-state">
        Nie ma jeszcze wyników do wyświetlenia.
      </p>
    </section>

    <!-- zgłaszane błędy pojawią się tutaj -->
    <section v-if="activeTab === 'feedbacks'" class="space-y-4">
      <div>
        <p class="text-sm text-muted">Kontakt od kursantów</p>
        <h2 class="section-title">Zgłoszone błędy i opinie</h2>
      </div>
      <div v-if="feedbacks.length" class="table-wrap">
        <table class="table min-w-[700px]">
          <thead>
            <tr>
              <th>Użytkownik</th>
              <th>Typ</th>
              <th>Treść wiadomości</th>
              <th>Data</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="fb in feedbacks" :key="fb.id">
              <td class="font-medium">{{ fb.username }}</td>
              <td>
                <span class="badge" :class="fb.lessonBlockId ? 'badge-danger' : 'badge-info'">
                  {{ fb.type }}
                </span>
              </td>
              <td>{{ fb.message }}</td>
              <td class="text-sm text-muted">{{ new Date(fb.createdAt).toLocaleString() }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-else class="empty-state">
        Brak zgłoszeń i opinii.
      </p>
    </section>

    <section v-if="activeTab === 'mentorApplications'" class="space-y-4">
      <div>
        <p class="text-sm text-muted">Zgłoszenia</p>
        <h2 class="section-title">Kandydaci na mentorów</h2>
      </div>
      <p v-if="reviewError" role="alert" class="alert alert-error">{{ reviewError }}</p>
      <p v-if="mentorApplicationsError" role="alert" class="alert alert-error">
        {{ mentorApplicationsError }} Sprawdź logowanie administratora i odpowiedź żądania <code class="font-mono">/api/admin/mentor-applications</code> w Network.
      </p>
      <div v-else-if="mentorApplications.length" class="table-wrap">
        <table class="table min-w-[900px]">
          <thead>
            <tr>
              <th>Kandydat</th>
              <th>Doświadczenie</th>
              <th>Portfolio</th>
              <th>Status</th>
              <th>Data zgłoszenia</th>
              <th>Decyzja</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="application in mentorApplications" :key="application.id" class="align-top">
              <td>
                <p class="font-semibold">{{ application.firstName }} {{ application.lastName }}</p>
                <p class="mt-1 text-sm text-muted">{{ application.username }} · {{ application.email }}</p>
              </td>
              <td class="max-w-lg whitespace-pre-wrap text-sm">{{ application.experience }}</td>
              <td>
                <a v-if="application.portfolioUrl" :href="application.portfolioUrl" target="_blank" rel="noopener noreferrer" class="font-medium text-paprika-600 underline">Otwórz portfolio</a>
                <span v-else class="text-muted">Brak</span>
              </td>
              <td>
                <span class="badge badge-info">{{ application.status === 'Pending' ? 'Oczekuje' : application.status }}</span>
              </td>
              <td class="whitespace-nowrap text-sm">{{ new Date(application.submittedAt).toLocaleString() }}</td>
              <td>
                <div v-if="application.status === 'Pending'" class="flex gap-2">
                  <button
                    type="button"
                    :disabled="reviewingApplicationId === application.id"
                    class="btn btn-success btn-sm"
                    @click="reviewMentorApplication(application.id, 'Approved')"
                  >Zatwierdź</button>
                  <button
                    type="button"
                    :disabled="reviewingApplicationId === application.id"
                    class="btn btn-danger btn-sm"
                    @click="reviewMentorApplication(application.id, 'Rejected')"
                  >Odrzuć</button>
                </div>
                <span v-else class="text-sm text-muted">Rozpatrzono</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-else-if="!mentorApplicationsError" class="empty-state">
        Nie ma jeszcze zgłoszeń na mentora.
      </p>
    </section>
  </SidebarLayout>
</template>
