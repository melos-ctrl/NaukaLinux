<script lang="ts" setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import { useRouter } from 'vue-router'
import PageHeader from '@/components/PageHeader.vue'

const router = useRouter()

interface Course {
  id: number
  title: string
  description?: string
}

const courses = ref<Course[]>([])
const isLoading = ref(true)
const errorMessage = ref('')
const isLoggedIn = ref(false)

const searchQuery = ref('')
const sortBy = ref('newest')

const fetchCourses = async () => {
  isLoading.value = true
  errorMessage.value = ''

  try {
    const token = localStorage.getItem('token')
    isLoggedIn.value = !!token

    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {}

    const response = await axios.get('/api/courses/published', config)
    courses.value = response.data
  } catch (error) {
    console.error(error)
    errorMessage.value = "Nie udało się załadować listy kursów."
  } finally {
    isLoading.value = false
  }
}

const filteredCourses = computed(() => {
  let result = courses.value

  if (searchQuery.value) {
    const query = searchQuery.value.toLowerCase()
    result = result.filter(c =>
      c.title.toLowerCase().includes(query) ||
      (c.description && c.description.toLowerCase().includes(query))
    )
  }

  if (sortBy.value === 'a-z') {
    result = result.slice().sort((a, b) => a.title.localeCompare(b.title))
  } else if (sortBy.value === 'z-a') {
    result = result.slice().sort((a, b) => b.title.localeCompare(a.title))
  } else if (sortBy.value === 'newest') {
    result = result.slice().sort((a, b) => b.id - a.id)
  } else if (sortBy.value === 'oldest') {
    result = result.slice().sort((a, b) => a.id - b.id)
  }

  return result
})

const goToLogin = () => {
  router.push('/login')
}

const startCourse = async (courseId: number) => {
  try {
    const token = localStorage.getItem('token')
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {}

    await axios.post(`/api/courses/${courseId}/enroll`, {}, config)

    router.push(`/course/${courseId}`)

  } catch (error) {
    console.error("Błąd podczas zapisu na kurs:", error)
    alert("Nie udało się rozpocząć kursu. Upewnij się, że jesteś zalogowany.")
  }
}

onMounted(() => {
  fetchCourses()
})
</script>

<template>
  <div class="page">
    <PageHeader
      eyebrow="Katalog"
      title="Kursy Linuxa"
      lead="Wybierz kurs i zacznij ćwiczyć w prawdziwym terminalu."
    />

    <!-- Filtry -->
    <div class="card flex flex-wrap items-end gap-4 md:p-5">
      <div class="min-w-64 flex-1">
        <label for="course-search" class="label">Szukaj kursu</label>
        <input
          id="course-search"
          v-model="searchQuery"
          type="search"
          placeholder="Wpisz tytuł lub opis"
          class="input"
        />
      </div>

      <div class="w-full sm:w-56">
        <label for="course-sort" class="label">Sortowanie</label>
        <select id="course-sort" v-model="sortBy" class="input cursor-pointer">
          <option value="newest">Od najnowszych</option>
          <option value="oldest">Od najstarszych</option>
          <option value="a-z">Alfabetycznie (A-Z)</option>
          <option value="z-a">Alfabetycznie (Z-A)</option>
        </select>
      </div>

      <button
        type="button"
        @click="searchQuery = ''; sortBy = 'newest'"
        class="btn btn-outline"
      >
        Wyczyść filtry
      </button>
    </div>

    <div v-if="isLoading" class="py-20 text-center text-lg font-medium text-muted animate-pulse" role="status">
      Wczytywanie kursów...
    </div>

    <div v-else-if="errorMessage" class="flex flex-col items-center gap-4 py-20">
      <p class="alert alert-error">{{ errorMessage }}</p>
      <button type="button" @click="fetchCourses" class="btn btn-dark">
        Spróbuj ponownie
      </button>
    </div>

    <p v-else-if="filteredCourses.length === 0" class="empty-state">
      Brak dostępnych kursów.
    </p>

    <div v-else class="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
      <article
        v-for="course in filteredCourses"
        :key="course.id"
        class="group flex flex-col overflow-hidden rounded-2xl border border-line bg-white shadow-sm transition-shadow hover:shadow-md"
      >
        <div class="flex h-36 items-center justify-center bg-[#f1ede6] font-mono text-3xl text-label transition-colors group-hover:bg-silver/40" aria-hidden="true">
          &gt;_
        </div>

        <div class="flex flex-1 flex-col p-5">
          <h3 class="mb-2 line-clamp-2 text-lg font-bold" :title="course.title">
            {{ course.title }}
          </h3>
          <p class="mb-4 line-clamp-3 flex-1 text-sm text-muted">
            {{ course.description || 'Brak opisu dla tego kursu.' }}
          </p>

          <button
            v-if="isLoggedIn"
            type="button"
            @click="startCourse(course.id)"
            class="btn btn-dark mt-auto w-full"
          >
            Rozpocznij naukę
          </button>
          <button
            v-else
            type="button"
            @click="goToLogin"
            class="btn btn-outline mt-auto w-full"
          >
            Zaloguj się, aby się zapisać
          </button>
        </div>
      </article>
    </div>
  </div>
</template>
