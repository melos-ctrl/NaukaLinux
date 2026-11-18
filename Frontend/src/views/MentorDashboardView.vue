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
  mentorName?: string;
}

interface MentorResult {
  id: number;
  username: string;
  courseTitle: string;
  score: number;
  maxScore: number;
  lastCompletedLessonId: number;
  completedLessonTitle?: string | null;
  completedModuleTitle?: string | null;
  totalLessons: number;
  completedLessons: number;
  updatedAt: string;
}

interface QuestionStatistics {
  blockId: number;
  lessonTitle: string;
  questionText: string;
  questionType: string;
  responseCount: number;
  options: Array<{
    optionId: number;
    optionText: string;
    selectionCount: number;
    percentage: number;
    students: string[];
  }>;
  textAnswers: Array<{
    answerText: string;
    selectionCount: number;
    percentage: number;
    students: string[];
  }>;
}

interface CourseStatistics {
  courseId: number;
  courseTitle: string;
  questions: QuestionStatistics[];
}

const courses = ref<Course[]>([]);
const results = ref<MentorResult[]>([]);
const feedbacks = ref<any[]>([]);
const statistics = ref<CourseStatistics | null>(null);
const statisticsCourseId = ref<number | null>(null);
const statisticsLoading = ref(false);
const statisticsError = ref('');

const isLoading = ref(true);
const errorMessage = ref('');
const activeTab = ref<'drafts' | 'published' | 'results' | 'feedbacks' | 'statistics'>('drafts');
const isAdmin = localStorage.getItem('role')?.toLowerCase() === 'admin';

const getConfig = () => {
  const token = localStorage.getItem('token');
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {};
};

// Procent ukończonych lekcji kursu
const progressPercent = (r: MentorResult) =>
  r.totalLessons > 0 ? Math.round((r.completedLessons / r.totalLessons) * 100) : 0;

const formatDate = (value: string) =>
  new Date(value).toLocaleString('pl-PL', {
    day: 'numeric',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  });

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

const loadStatistics = async (courseId: number) => {
  statisticsCourseId.value = courseId;
  statisticsLoading.value = true;
  statisticsError.value = '';

  try {
    const response = await axios.get(`/api/courses/${courseId}/statistics`, getConfig());
    statistics.value = response.data as CourseStatistics;
  } catch (error) {
    console.error('Błąd podczas pobierania statystyk kursu:', error);
    statistics.value = null;
    statisticsError.value = 'Nie udało się pobrać statystyk tego kursu.';
  } finally {
    statisticsLoading.value = false;
  }
};

const openStatistics = (courseId?: number) => {
  activeTab.value = 'statistics';
  const selectedCourseId = courseId ?? statisticsCourseId.value ?? courses.value[0]?.id;

  if (selectedCourseId) {
    void loadStatistics(selectedCourseId);
  } else {
    statistics.value = null;
    statisticsLoading.value = false;
    statisticsError.value = 'Nie masz jeszcze żadnego kursu do sprawdzenia.';
  }
};

const changeStatisticsCourse = (event: Event) => {
  const courseId = Number((event.target as HTMLSelectElement).value);
  if (Number.isInteger(courseId) && courseId > 0) void loadStatistics(courseId);
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
        <button
          @click="openStatistics()"
          class="pb-3 px-2 font-bold transition-all relative border-b-2"
          :class="activeTab === 'statistics' ? 'text-spicy-paprika border-spicy-paprika' : 'text-gray-500 border-transparent hover:text-charcoal-brown'"
        >
          Statystyki
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
                <p v-if="isAdmin" class="text-xs text-gray-500 mb-2">Autor: {{ course.mentorName || 'Nieznany autor' }}</p>
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
                <p v-if="isAdmin" class="text-xs text-gray-500 mb-2">Autor: {{ course.mentorName || 'Nieznany autor' }}</p>
                <p class="text-sm text-gray-600 line-clamp-2">{{ course.description || 'Brak opisu.' }}</p>
              </div>
              <div class="mt-6 pt-4 border-t border-gray-100 flex justify-end gap-2">
                <button @click="openStatistics(course.id)" class="px-4 py-2 border border-spicy-paprika text-spicy-paprika rounded hover:bg-spicy-paprika hover:text-white transition-colors text-sm font-bold">
                  Statystyki
                </button>
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
            <table class="w-full min-w-[760px] border-collapse text-left">
              <thead class="bg-floral-white text-sm text-charcoal-brown/70">
                <tr>
                  <th class="px-5 py-4 font-semibold">Kursant</th>
                  <th class="px-5 py-4 font-semibold">Kurs</th>
                  <th class="px-5 py-4 font-semibold w-64">Postęp</th>
                  <th class="px-5 py-4 font-semibold">Wynik</th>
                  <th class="px-5 py-4 font-semibold">Ostatnia aktywność</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-silver/40">
                <tr v-for="r in results" :key="r.id" class="align-top">
                  <td class="px-5 py-4 font-medium">{{ r.username }}</td>
                  <td class="px-5 py-4">{{ r.courseTitle }}</td>

                  <td class="px-5 py-4">
                    <div class="flex items-center justify-between gap-2 text-sm font-semibold">
                      <span>{{ r.completedLessons }} z {{ r.totalLessons }} lekcji</span>
                      <span :class="progressPercent(r) === 100 ? 'text-green-600' : 'text-charcoal-brown/70'">
                        {{ progressPercent(r) === 100 ? 'Ukończony ✓' : `${progressPercent(r)}%` }}
                      </span>
                    </div>
                    <div
                      class="mt-1.5 h-2 overflow-hidden rounded-full bg-silver/30"
                      role="progressbar"
                      :aria-valuenow="progressPercent(r)"
                      aria-valuemin="0"
                      aria-valuemax="100"
                    >
                      <div
                        class="h-full rounded-full transition-all"
                        :class="progressPercent(r) === 100 ? 'bg-green-600' : 'bg-spicy-paprika'"
                        :style="{ width: `${progressPercent(r)}%` }"
                      ></div>
                    </div>
                    <p v-if="r.completedLessonTitle" class="mt-1.5 text-xs text-gray-500">
                      Ostatnio: {{ r.completedLessonTitle }}<span v-if="r.completedModuleTitle"> · {{ r.completedModuleTitle }}</span>
                    </p>
                    <p v-else-if="r.lastCompletedLessonId === 0" class="mt-1.5 text-xs text-gray-400">
                      Jeszcze nie ukończył żadnej lekcji
                    </p>
                  </td>

                  <td class="px-5 py-4 whitespace-nowrap">
                    <span class="font-bold text-green-600">{{ r.score }}</span>
                    <span v-if="r.maxScore > 0" class="text-sm text-gray-500"> / {{ r.maxScore }} pkt</span>
                    <span v-else class="text-sm text-gray-500"> pkt</span>
                  </td>

                  <td class="px-5 py-4 text-sm text-gray-500 whitespace-nowrap">{{ formatDate(r.updatedAt) }}</td>
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

        <!-- 5. Statystyki odpowiedzi -->
        <div v-if="activeTab === 'statistics'" class="space-y-6">
          <div class="flex flex-wrap items-end justify-between gap-4">
            <div>
              <p class="text-sm font-medium text-gray-500">Odpowiedzi kursantów</p>
              <h2 class="text-xl font-bold text-charcoal-brown">Statystyki kursu</h2>
            </div>
            <label class="flex min-w-56 flex-col gap-1 text-sm font-semibold text-charcoal-brown">
              Kurs
              <select
                :value="statisticsCourseId ?? ''"
                class="rounded-lg border border-silver/70 bg-white px-3 py-2"
                :disabled="courses.length === 0"
                @change="changeStatisticsCourse"
              >
                <option value="" disabled>Wybierz kurs</option>
                <option v-for="course in courses" :key="course.id" :value="course.id">
                  {{ course.title }}
                </option>
              </select>
            </label>
          </div>

          <p v-if="statisticsLoading" class="py-8 text-center font-semibold text-gray-500" role="status">
            Ładowanie statystyk...
          </p>
          <p v-else-if="statisticsError" class="rounded-lg bg-red-50 p-4 text-red-700" role="alert">
            {{ statisticsError }}
          </p>
          <div v-else-if="statistics" class="space-y-4">
            <p class="text-sm text-gray-600">
              Procent liczony jest wśród osób, które odpowiedziały na dane pytanie. Przy pytaniach wielokrotnego wyboru suma procentów może przekraczać 100%.
            </p>
            <div v-if="statistics.questions.length" class="space-y-4">
              <article
                v-for="question in statistics.questions"
                :key="question.blockId"
                class="rounded-xl border border-silver/50 bg-white p-5 shadow-sm"
              >
                <div class="flex flex-wrap items-start justify-between gap-3">
                  <div>
                    <p class="text-sm font-semibold text-spicy-paprika">{{ question.lessonTitle }}</p>
                    <h3 class="mt-1 break-words font-bold text-charcoal-brown">{{ question.questionText }}</h3>
                  </div>
                  <span class="shrink-0 rounded-full bg-silver/20 px-3 py-1 text-sm text-charcoal-brown">
                    {{ question.responseCount }} odp.
                  </span>
                </div>

                <div v-if="question.options.length" class="mt-5 space-y-4">
                  <div v-for="option in question.options" :key="option.optionId" class="space-y-1.5">
                    <div class="flex flex-wrap justify-between gap-2 text-sm">
                      <span class="font-medium text-charcoal-brown">{{ option.optionText }}</span>
                      <span class="font-semibold text-charcoal-brown">
                        {{ option.percentage }}% ({{ option.selectionCount }})
                      </span>
                    </div>
                    <div class="h-2 overflow-hidden rounded-full bg-silver/30" role="presentation">
                      <div
                        class="h-full rounded-full bg-spicy-paprika"
                        :style="{ width: `${Math.min(option.percentage, 100)}%` }"
                      ></div>
                    </div>
                    <p class="break-words text-sm text-gray-600">
                      Wybrali: {{ option.students.length ? option.students.join(', ') : 'nikt' }}
                    </p>
                  </div>
                </div>

                <div v-if="question.textAnswers.length" class="mt-5 space-y-3">
                  <h4 class="text-sm font-bold text-charcoal-brown">Odpowiedzi tekstowe</h4>
                  <div v-for="answer in question.textAnswers" :key="answer.answerText" class="rounded-lg bg-floral-white p-3">
                    <div class="flex flex-wrap justify-between gap-2 text-sm">
                      <span class="break-words font-medium text-charcoal-brown">{{ answer.answerText }}</span>
                      <span class="shrink-0 font-semibold text-charcoal-brown">
                        {{ answer.percentage }}% ({{ answer.selectionCount }})
                      </span>
                    </div>
                    <p class="mt-1 break-words text-sm text-gray-600">
                      Odpowiedzieli: {{ answer.students.join(', ') }}
                    </p>
                  </div>
                </div>

                <p v-if="!question.responseCount" class="mt-4 text-sm text-gray-500">
                  Nikt jeszcze nie odpowiedział na to pytanie.
                </p>
              </article>
            </div>
            <p v-else class="rounded-xl border border-silver/50 bg-white p-6 text-gray-600">
              Ten kurs nie zawiera pytań do statystyk.
            </p>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>