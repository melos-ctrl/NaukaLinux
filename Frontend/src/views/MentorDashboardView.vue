<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';

const router = useRouter();

// Interfejs kursu
interface Course {
  id: number;
  title: string;
  description?: string;
  isPublished: boolean;
}

const courses = ref<Course[]>([]);
const isLoading = ref(true);
const errorMessage = ref('');
const activeTab = ref<'drafts' | 'published'>('drafts');

const fetchMentorCourses = async () => {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const token = localStorage.getItem('token');
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {};

    const response = await axios.get('/api/courses/my-courses', config);
    courses.value = response.data;
  } catch (error) {
    console.error("Błąd podczas pobierania kursów:", error);
    errorMessage.value = "Nie udało się pobrać Twoich kursów. Upewnij się, że jesteś zalogowany.";
  } finally {
    isLoading.value = false;
  }
};

const draftCourses = computed(() => courses.value.filter(c => !c.isPublished));
const publishedCourses = computed(() => courses.value.filter(c => c.isPublished));

const editCourse = (courseId: number) => {
  router.push(`/course-creator/${courseId}`);
};

const createNewCourse = () => {
  router.push('/course-creator');
};

onMounted(() => {
  fetchMentorCourses();
});
</script>

<template>
  <div class="min-h-screen bg-floral-white text-charcoal-brown p-8 flex flex-col items-center">
    
    <div class="w-full max-w-5xl">
      <!-- nagłówek -->
      <header class="flex justify-between items-center bg-charcoal-brown text-floral-white p-6 rounded-xl shadow-md mb-8">
        <div>
          <h1 class="text-2xl font-bold">Panel Mentora</h1>
          <p class="text-sm text-silver mt-1">Zarządzaj swoimi kursami i twórz nowe materiały</p>
        </div>
        <button 
          @click="createNewCourse"
          class="px-5 py-2.5 bg-spicy-paprika hover:opacity-90 text-floral-white font-bold rounded-lg shadow-md transition-all flex items-center gap-2"
        >
          + Stwórz nowy kurs
        </button>
      </header>

      <!-- zakładki -->
      <div class="flex gap-4 border-b border-silver/50 mb-8">
        <button 
          @click="activeTab = 'drafts'"
          class="pb-3 px-2 font-bold transition-all relative border-b-2"
          :class="activeTab === 'drafts' ? 'text-spicy-paprika border-spicy-paprika' : 'text-gray-500 border-transparent hover:text-charcoal-brown'"
        >
          Wersje robocze ({{ draftCourses.length }})
        </button>
        <button 
          @click="activeTab = 'published'"
          class="pb-3 px-2 font-bold transition-all relative border-b-2"
          :class="activeTab === 'published' ? 'text-spicy-paprika border-spicy-paprika' : 'text-gray-500 border-transparent hover:text-charcoal-brown'"
        >
          Opublikowane ({{ publishedCourses.length }})
        </button>
      </div>

      <!-- komunikaty (Ładowanie / Błąd) -->
      <div v-if="isLoading" class="text-center py-10 font-bold text-gray-500 animate-pulse">
        Ładowanie Twoich materiałów
      </div>
      <div v-else-if="errorMessage" class="bg-red-100 text-red-600 p-4 rounded-lg font-bold text-center">
        {{ errorMessage }}
      </div>

      <!-- lista kursów -->
      <div v-else>
        
        <!-- wersje robocze -->
        <div v-if="activeTab === 'drafts'">
          <div v-if="draftCourses.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            Nie masz żadnych wersji roboczych.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div v-for="course in draftCourses" :key="course.id" class="bg-white border border-silver/50 p-6 rounded-xl shadow-sm hover:shadow-md transition-all flex flex-col justify-between">
              <div>
                <span class="inline-block px-2 py-1 bg-yellow-100 text-yellow-700 text-xs font-bold uppercase rounded mb-3">Szkic</span>
                <h3 class="text-lg font-bold mb-2">{{ course.title }}</h3>
                <p class="text-sm text-gray-600 line-clamp-2">{{ course.description || 'Brak opisu.' }}</p>
              </div>
              <div class="mt-6 pt-4 border-t border-gray-100 text-right">
                <button @click="editCourse(course.id)" class="px-4 py-2 bg-charcoal-brown text-floral-white rounded hover:bg-spicy-paprika transition-colors text-sm font-bold">
                  Edytuj zawartość
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- opublikowane kursy -->
        <div v-if="activeTab === 'published'">
          <div v-if="publishedCourses.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            Nie masz jeszcze żadnych opublikowanych kursów.
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div v-for="course in publishedCourses" :key="course.id" class="bg-white border border-silver/50 p-6 rounded-xl shadow-sm hover:shadow-md transition-all flex flex-col justify-between">
              <div>
                <span class="inline-block px-2 py-1 bg-green-100 text-green-700 text-xs font-bold uppercase rounded mb-3">Opublikowany</span>
                <h3 class="text-lg font-bold mb-2">{{ course.title }}</h3>
                <p class="text-sm text-gray-600 line-clamp-2">{{ course.description || 'Brak opisu.' }}</p>
              </div>
              <div class="mt-6 pt-4 border-t border-gray-100 text-right">
                <button @click="editCourse(course.id)" class="px-4 py-2 bg-charcoal-brown text-floral-white rounded hover:bg-spicy-paprika transition-colors text-sm font-bold">
                  Zarządzaj
                </button>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>