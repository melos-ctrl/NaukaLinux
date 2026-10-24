<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';

const router = useRouter();

interface Course {
  id: number;
  title: string;
  description?: string;
  isPublished: boolean;
}

const courses = ref<Course[]>([]);
const results = ref<any[]>([]);
const feedbacks = ref<any[]>([]);

const isLoading = ref(true);
const errorMessage = ref('');
const activeTab = ref<'drafts' | 'published' | 'results' | 'feedbacks'>('drafts');
const isAdmin = localStorage.getItem('role')?.toLowerCase() === 'admin';

const getConfig = () => {
  const token = localStorage.getItem('token');
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {};
};

const fetchMentorData = async () => {
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const config = getConfig();
    const [cRes, rRes, fRes] = await Promise.all([
      axios.get('/api/courses/my-courses', config),
      axios.get('/api/courses/mentor-results', config),
      axios.get('/api/courses/mentor-feedbacks', config)
    ]);

    courses.value = cRes.data;
    results.value = rRes.data;
    feedbacks.value = fRes.data;
  } catch (error) {
    console.error("Błąd podczas pobierania danych mentora:", error);
    errorMessage.value = "Nie udało się pobrać danych. Upewnij się, że masz uprawnienia Mentora.";
  } finally {
    isLoading.value = false;
  }
};

const draftCourses = computed(() => courses.value.filter(c => !c.isPublished));
const publishedCourses = computed(() => courses.value.filter(c => c.isPublished));

const editCourse = (courseId: number) => {
  router.push(`/course-creator/${courseId}`);
};

const deleteCourse = async (courseId: number) => {
  if (!confirm('Czy na pewno chcesz usunąć ten kurs?')) return;
  try {
    await axios.delete(`/api/courses/${courseId}`, getConfig());
    courses.value = courses.value.filter(c => c.id !== courseId);
  } catch (error) {
    alert('Nie udało się usunąć kursu.');
  }
};

const createNewCourse = () => {
  router.push('/course-creator');
};

onMounted(() => {
  fetchMentorData();
});
</script>

<template>
  <div class="min-h-screen bg-floral-white text-charcoal-brown p-8 flex flex-col items-center">
    
    <div class="w-full max-w-5xl">
      <!-- nagłówek -->
      <header class="flex justify-between items-center bg-charcoal-brown text-floral-white p-6 rounded-xl shadow-md mb-8">
        <div>
          <h1 class="text-2xl font-bold">{{ isAdmin ? 'Zarządzanie kursami' : 'Panel Mentora' }}</h1>
          <p class="text-sm text-silver mt-1">
            {{ isAdmin ? 'Przeglądaj i edytuj kursy na platformie' : 'Zarządzaj swoimi kursami, sprawdzaj wyniki uczniów i reaguj na zgłoszenia' }}
          </p>
        </div>
        <button 
          @click="createNewCourse"
          class="px-5 py-2.5 bg-spicy-paprika hover:opacity-90 text-floral-white font-bold rounded-lg shadow-md transition-all flex items-center gap-2"
        >
          + Stwórz nowy kurs
        </button>
      </header>

      <!-- zakładki -->
      <div class="flex flex-wrap gap-4 border-b border-silver/50 mb-8">
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
        <button 
          @click="activeTab = 'results'"
          class="pb-3 px-2 font-bold transition-all relative border-b-2"
          :class="activeTab === 'results' ? 'text-spicy-paprika border-spicy-paprika' : 'text-gray-500 border-transparent hover:text-charcoal-brown'"
        >
          Wyniki kursantów ({{ results.length }})
        </button>
        <button 
          @click="activeTab = 'feedbacks'"
          class="pb-3 px-2 font-bold transition-all relative border-b-2"
          :class="activeTab === 'feedbacks' ? 'text-spicy-paprika border-spicy-paprika' : 'text-gray-500 border-transparent hover:text-charcoal-brown'"
        >
          Zgłoszenia i opinie ({{ feedbacks.length }})
        </button>
      </div>

      <!-- komunikaty (Ładowanie / Błąd) -->
      <div v-if="isLoading" class="text-center py-10 font-bold text-gray-500 animate-pulse">
        Ładowanie Twoich materiałów...
      </div>
      <div v-else-if="errorMessage" class="bg-red-100 text-red-600 p-4 rounded-lg font-bold text-center">
        {{ errorMessage }}
      </div>

      <!-- zawartość zakładek -->
      <div v-else>
        
        <!-- 1. Wersje robocze -->
        <div v-if="activeTab === 'drafts'">
          <div v-if="draftCourses.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            {{ isAdmin ? 'Brak wersji roboczych.' : 'Nie masz żadnych wersji roboczych.' }}
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div v-for="course in draftCourses" :key="course.id" class="bg-white border border-silver/50 p-6 rounded-xl shadow-sm hover:shadow-md transition-all flex flex-col justify-between">
              <div>
                <span class="inline-block px-2 py-1 bg-yellow-100 text-yellow-700 text-xs font-bold uppercase rounded mb-3">Szkic</span>
                <h3 class="text-lg font-bold mb-2">{{ course.title }}</h3>
                <p class="text-sm text-gray-600 line-clamp-2">{{ course.description || 'Brak opisu.' }}</p>
              </div>
              <div class="mt-6 pt-4 border-t border-gray-100 flex justify-end gap-2">
                <button @click="editCourse(course.id)" class="px-4 py-2 bg-charcoal-brown text-floral-white rounded hover:bg-spicy-paprika transition-colors text-sm font-bold">
                  Edytuj zawartość
                </button>
                <button @click="deleteCourse(course.id)" class="px-3 py-2 border border-silver/70 text-charcoal-brown rounded hover:border-red-500 hover:text-red-600 transition-colors text-sm font-bold">
                  Usuń
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- 2. Opublikowane kursy -->
        <div v-if="activeTab === 'published'">
          <div v-if="publishedCourses.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            {{ isAdmin ? 'Brak opublikowanych kursów.' : 'Nie masz jeszcze żadnych opublikowanych kursów.' }}
          </div>
          <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div v-for="course in publishedCourses" :key="course.id" class="bg-white border border-silver/50 p-6 rounded-xl shadow-sm hover:shadow-md transition-all flex flex-col justify-between">
              <div>
                <span class="inline-block px-2 py-1 bg-green-100 text-green-700 text-xs font-bold uppercase rounded mb-3">Opublikowany</span>
                <h3 class="text-lg font-bold mb-2">{{ course.title }}</h3>
                <p class="text-sm text-gray-600 line-clamp-2">{{ course.description || 'Brak opisu.' }}</p>
              </div>
              <div class="mt-6 pt-4 border-t border-gray-100 flex justify-end gap-2">
                <button @click="editCourse(course.id)" class="px-4 py-2 bg-charcoal-brown text-floral-white rounded hover:bg-spicy-paprika transition-colors text-sm font-bold">
                  Zarządzaj
                </button>
                <button @click="deleteCourse(course.id)" class="px-3 py-2 border border-silver/70 text-charcoal-brown rounded hover:border-red-500 hover:text-red-600 transition-colors text-sm font-bold">
                  Usuń
                </button>
              </div>
            </div>
          </div>
        </div>

        <!-- 3. Wyniki kursantów -->
        <div v-if="activeTab === 'results'">
          <div v-if="results.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            Nikt jeszcze nie rozwiązał zadań w Twoich kursach.
          </div>
          <div v-else class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
            <table class="w-full min-w-[600px] border-collapse text-left">
              <thead class="bg-floral-white text-sm text-charcoal-brown/70">
                <tr>
                  <th class="px-5 py-4 font-semibold">Uczestnik</th>
                  <th class="px-5 py-4 font-semibold">Kurs</th>
                  <th class="px-5 py-4 font-semibold">Ostatnia lekcja</th>
                  <th class="px-5 py-4 font-semibold">Wynik</th>
                  <th class="px-5 py-4 font-semibold">Data</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-silver/40">
                <tr v-for="r in results" :key="r.id">
                  <td class="px-5 py-4 font-medium">{{ r.username }}</td>
                  <td class="px-5 py-4">{{ r.courseTitle }}</td>
                  <td class="px-5 py-4">Lekcja {{ r.lastCompletedLessonId }}</td>
                  <td class="px-5 py-4 font-bold text-green-600">{{ r.score }} pkt</td>
                  <td class="px-5 py-4 text-sm text-gray-500">{{ new Date(r.updatedAt).toLocaleString() }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- 4. Zgłoszenia błędów i opinie -->
        <div v-if="activeTab === 'feedbacks'">
          <div v-if="feedbacks.length === 0" class="text-center py-12 text-gray-500 border-2 border-dashed border-silver rounded-xl">
            Brak zgłoszonych błędów i opinii do Twoich kursów.
          </div>
          <div v-else class="overflow-x-auto rounded-xl border border-silver/50 bg-white shadow-sm">
            <table class="w-full min-w-[600px] border-collapse text-left">
              <thead class="bg-floral-white text-sm text-charcoal-brown/70">
                <tr>
                  <th class="px-5 py-4 font-semibold">Użytkownik</th>
                  <th class="px-5 py-4 font-semibold">Typ</th>
                  <th class="px-5 py-4 font-semibold">Treść</th>
                  <th class="px-5 py-4 font-semibold">Akcja</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-silver/40">
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
                  <td class="px-5 py-4">
                    <button 
                      @click="editCourse(fb.courseId)" 
                      class="text-xs font-bold text-spicy-paprika hover:underline"
                    >
                      Przejdź do kursu →
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>