<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from 'axios'
import { useRouter } from 'vue-router'

const router = useRouter()

const courses = ref([])
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

    const response = await axios.get('http://localhost:5042/api/courses/published', config)
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

const startCourse = async (courseId) => {
  try {
    const token = localStorage.getItem('token')
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {}

    await axios.post(`http://localhost:5042/api/courses/${courseId}/enroll`, {}, config)

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
  <div class="min-h-screen font-sans flex flex-col">
    <div class="flex-1 flex flex-col md:flex-row w-full">
      <main class="flex-1 bg-floral-white p-6 md:p-8 text-charcoal-brown">
        <div class="max-w-5xl mx-auto">
          <div v-if="isLoading" class="flex justify-center items-center py-20">
            <span class="text-silver text-lg font-medium animate-pulse">Wczytywanie</span>
          </div>

          <div v-else-if="errorMessage" class="flex justify-center items-center py-20 flex-col gap-4">
            <p class="text-spicy-paprika text-lg font-medium">{{ errorMessage }}</p>
            <button @click="fetchCourses" class="px-4 py-2 bg-charcoal-brown text-white rounded-lg hover:bg-carbon-black transition-colors">
              Spróbuj ponownie
            </button>
          </div>

          <div v-else-if="filteredCourses.length === 0" class="flex justify-center items-center py-20">
            <p class="text-silver text-lg">Brak dostępnych kursów.</p>
          </div>

          <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <div 
              v-for="course in filteredCourses" 
              :key="course.id"
              class="bg-white border border-silver/50 rounded-xl shadow-sm hover:shadow-md transition-shadow flex flex-col overflow-hidden group"
            >
              <div class="h-40 bg-silver/20 flex items-center justify-center group-hover:bg-silver/30 transition-colors">
              </div>

              <div class="p-5 flex flex-col flex-1">
                <h3 class="text-lg font-bold text-charcoal-brown mb-2 line-clamp-2" :title="course.title">
                  {{ course.title }}
                </h3>
                <p class="text-sm text-silver line-clamp-3 mb-4 flex-1">
                  {{ course.description || 'Brak opisu dla tego kursu.' }}
                </p>

                <button 
                  v-if="isLoggedIn" 
                  @click="startCourse(course.id)"
                  class="w-full px-4 py-2 mt-auto bg-charcoal-brown hover:bg-spicy-paprika text-white font-medium rounded-lg transition-colors shadow-sm"
                >
                  Rozpocznij naukę
                </button>
                <button 
                  v-else 
                  @click="goToLogin"
                  class="w-full px-4 py-2 mt-auto bg-silver hover:bg-silver/80 text-white font-medium rounded-lg transition-colors shadow-sm"
                >
                  Zaloguj się, aby się zapisać
                </button>
              </div>
            </div>
          </div>
        </div>
      </main>

      <aside class="w-full md:w-1/5 bg-charcoal-brown border-l border-silver/30 p-6 shrink-0">
        <div class="sticky top-24">
          <h2 class="text-lg font-bold text-floral-white mb-5 flex items-center gap-2">
            <span>Filtrowanie</span> 
          </h2>
          
          <div class="mb-5">
            <label class="block text-xs font-bold text-silver uppercase tracking-wider mb-2">Szukaj kursu</label>
            <input 
              v-model="searchQuery" 
              type="text" 
              placeholder="Wpisz tytuł lub opis" 
              class="w-full px-4 py-2 bg-floral-white border border-silver/50 text-charcoal-brown rounded-lg focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-sm transition-all"
            />
          </div>

          <div class="mb-6">
            <label class="block text-xs font-bold text-silver uppercase tracking-wider mb-2">Sortowanie</label>
            <select 
              v-model="sortBy" 
              class="w-full px-4 py-2 bg-floral-white border border-silver/50 text-charcoal-brown rounded-lg focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-sm transition-all cursor-pointer"
            >
              <option value="newest">Od najnowszych</option>
              <option value="oldest">Od najstarszych</option>
              <option value="a-z">Alfabetycznie (A-Z)</option>
              <option value="z-a">Alfabetycznie (Z-A)</option>
            </select>
          </div>
          
          <div class="border-t border-silver/30 pt-5">
            <button 
              @click="searchQuery = ''; sortBy = 'newest'" 
              class="w-full px-4 py-2 bg-carbon-black hover:bg-spicy-paprika hover:text-floral-white border border-silver/30 text-floral-white text-sm font-medium rounded-lg transition-colors"
            >
              Wyczyść filtry
            </button>
          </div>
        </div>
      </aside>
    </div>
  </div>
</template>