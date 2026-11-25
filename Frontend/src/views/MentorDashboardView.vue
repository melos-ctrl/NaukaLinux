<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';
import SidebarLayout from '@/components/SidebarLayout.vue';
import PageHeader from '@/components/PageHeader.vue';

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
  <SidebarLayout>
    <template #profile>
      <div class="flex h-20 w-20 items-center justify-center rounded-full border-4 border-spicy-paprika bg-carbon-black font-mono text-3xl font-bold shadow-lg" aria-hidden="true">
        {{ isAdmin ? 'A' : 'M' }}
      </div>
      <h2 class="mt-2 font-bold">{{ isAdmin ? 'Zarządzanie kursami' : 'Panel mentora' }}</h2>
      <p class="text-sm text-silver">{{ isAdmin ? 'Administrator' : 'Mentor' }}</p>
    </template>

    <template #nav>
      <button type="button" class="side-link shrink-0" :class="{ 'side-link-active': activeTab === 'drafts' }" @click="activeTab = 'drafts'">
        Wersje robocze ({{ draftCourses.length }})
      </button>
      <button type="button" class="side-link shrink-0" :class="{ 'side-link-active': activeTab === 'published' }" @click="activeTab = 'published'">
        Opublikowane ({{ publishedCourses.length }})
      </button>
      <button type="button" class="side-link shrink-0" :class="{ 'side-link-active': activeTab === 'results' }" @click="activeTab = 'results'">
        Wyniki kursantów ({{ results.length }})
      </button>
      <button type="button" class="side-link shrink-0" :class="{ 'side-link-active': activeTab === 'feedbacks' }" @click="activeTab = 'feedbacks'">
        Zgłoszenia i opinie ({{ feedbacks.length }})
      </button>
      <button type="button" class="side-link shrink-0" :class="{ 'side-link-active': activeTab === 'statistics' }" @click="openStatistics()">
        Statystyki
      </button>
    </template>

    <PageHeader
      :eyebrow="isAdmin ? 'Administracja' : 'Panel mentora'"
      :title="isAdmin ? 'Zarządzanie kursami' : 'Moje kursy'"
      :lead="isAdmin ? 'Przeglądaj i edytuj kursy na platformie.' : 'Zarządzaj swoimi kursami, sprawdzaj wyniki uczniów i reaguj na zgłoszenia.'"
    >
      <template #actions>
        <button type="button" @click="createNewCourse" class="btn btn-primary">
          + Stwórz nowy kurs
        </button>
      </template>
    </PageHeader>

    <!-- komunikaty (Ładowanie / Błąd) -->
    <div v-if="isLoading" class="py-10 text-center font-medium text-muted animate-pulse" role="status">
      Ładowanie Twoich materiałów...
    </div>
    <p v-else-if="errorMessage" class="alert alert-error" role="alert">
      {{ errorMessage }}
    </p>

    <!-- zawartość zakładek -->
    <div v-else>

      <!-- 1. Wersje robocze -->
      <div v-if="activeTab === 'drafts'">
        <p v-if="draftCourses.length === 0" class="empty-state">
          {{ isAdmin ? 'Brak wersji roboczych.' : 'Nie masz żadnych wersji roboczych.' }}
        </p>
        <div v-else class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <article v-for="course in draftCourses" :key="course.id" class="card card-hover flex flex-col justify-between md:p-6">
            <div>
              <span class="badge badge-warning mb-3">Szkic</span>
              <h3 class="mb-2 text-lg font-bold">{{ course.title }}</h3>
              <p v-if="isAdmin" class="mb-2 text-xs text-muted">Autor: {{ course.mentorName || 'Nieznany autor' }}</p>
              <p class="line-clamp-2 text-sm text-muted">{{ course.description || 'Brak opisu.' }}</p>
            </div>
            <div class="mt-6 flex justify-end gap-2 border-t border-line pt-4">
              <button type="button" @click="editCourse(course.id)" class="btn btn-dark btn-sm">
                Edytuj zawartość
              </button>
              <button type="button" @click="deleteCourse(course.id)" class="btn btn-danger btn-sm">
                Usuń
              </button>
            </div>
          </article>
        </div>
      </div>

      <!-- 2. Opublikowane kursy -->
      <div v-if="activeTab === 'published'">
        <p v-if="publishedCourses.length === 0" class="empty-state">
          {{ isAdmin ? 'Brak opublikowanych kursów.' : 'Nie masz jeszcze żadnych opublikowanych kursów.' }}
        </p>
        <div v-else class="grid grid-cols-1 gap-6 md:grid-cols-2">
          <article v-for="course in publishedCourses" :key="course.id" class="card card-hover flex flex-col justify-between md:p-6">
            <div>
              <span class="badge badge-success mb-3">Opublikowany</span>
              <h3 class="mb-2 text-lg font-bold">{{ course.title }}</h3>
              <p v-if="isAdmin" class="mb-2 text-xs text-muted">Autor: {{ course.mentorName || 'Nieznany autor' }}</p>
              <p class="line-clamp-2 text-sm text-muted">{{ course.description || 'Brak opisu.' }}</p>
            </div>
            <div class="mt-6 flex flex-wrap justify-end gap-2 border-t border-line pt-4">
              <button type="button" @click="openStatistics(course.id)" class="btn btn-outline btn-sm">
                Statystyki
              </button>
              <button type="button" @click="editCourse(course.id)" class="btn btn-dark btn-sm">
                Zarządzaj
              </button>
              <button type="button" @click="deleteCourse(course.id)" class="btn btn-danger btn-sm">
                Usuń
              </button>
            </div>
          </article>
        </div>
      </div>

      <!-- 3. Wyniki kursantów -->
      <div v-if="activeTab === 'results'">
        <p v-if="results.length === 0" class="empty-state">
          Nikt jeszcze nie rozwiązał zadań w Twoich kursach.
        </p>
        <div v-else class="table-wrap">
          <table class="table min-w-[760px]">
            <thead>
              <tr>
                <th>Kursant</th>
                <th>Kurs</th>
                <th class="w-64">Postęp</th>
                <th>Wynik</th>
                <th>Ostatnia aktywność</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="r in results" :key="r.id" class="align-top">
                <td class="font-medium">{{ r.username }}</td>
                <td>{{ r.courseTitle }}</td>

                <td>
                  <div class="flex items-center justify-between gap-2 text-sm font-semibold">
                    <span>{{ r.completedLessons }} z {{ r.totalLessons }} lekcji</span>
                    <span :class="progressPercent(r) === 100 ? 'text-green-700' : 'text-muted'">
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
                      :class="progressPercent(r) === 100 ? 'bg-green-700' : 'bg-spicy-paprika'"
                      :style="{ width: `${progressPercent(r)}%` }"
                    ></div>
                  </div>
                  <p v-if="r.completedLessonTitle" class="mt-1.5 text-xs text-muted">
                    Ostatnio: {{ r.completedLessonTitle }}<span v-if="r.completedModuleTitle"> · {{ r.completedModuleTitle }}</span>
                  </p>
                  <p v-else-if="r.lastCompletedLessonId === 0" class="mt-1.5 text-xs text-muted">
                    Jeszcze nie ukończył żadnej lekcji
                  </p>
                </td>

                <td class="whitespace-nowrap">
                  <span class="font-bold text-green-700">{{ r.score }}</span>
                  <span v-if="r.maxScore > 0" class="text-sm text-muted"> / {{ r.maxScore }} pkt</span>
                  <span v-else class="text-sm text-muted"> pkt</span>
                </td>

                <td class="whitespace-nowrap text-sm text-muted">{{ formatDate(r.updatedAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- 4. Zgłoszenia błędów i opinie -->
      <div v-if="activeTab === 'feedbacks'">
        <p v-if="feedbacks.length === 0" class="empty-state">
          Brak zgłoszonych błędów i opinii do Twoich kursów.
        </p>
        <div v-else class="table-wrap">
          <table class="table min-w-[600px]">
            <thead>
              <tr>
                <th>Użytkownik</th>
                <th>Typ</th>
                <th>Treść</th>
                <th>Akcja</th>
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
                <td>
                  <button
                    type="button"
                    @click="editCourse(fb.courseId)"
                    class="btn btn-ghost btn-sm"
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
            <p class="text-sm text-muted">Odpowiedzi kursantów</p>
            <h2 class="section-title">Statystyki kursu</h2>
          </div>
          <div class="min-w-56">
            <label for="statistics-course" class="label">Kurs</label>
            <select
              id="statistics-course"
              :value="statisticsCourseId ?? ''"
              class="input"
              :disabled="courses.length === 0"
              @change="changeStatisticsCourse"
            >
              <option value="" disabled>Wybierz kurs</option>
              <option v-for="course in courses" :key="course.id" :value="course.id">
                {{ course.title }}
              </option>
            </select>
          </div>
        </div>

        <p v-if="statisticsLoading" class="py-8 text-center font-medium text-muted animate-pulse" role="status">
          Ładowanie statystyk...
        </p>
        <p v-else-if="statisticsError" class="alert alert-error" role="alert">
          {{ statisticsError }}
        </p>
        <div v-else-if="statistics" class="space-y-4">
          <p class="text-sm text-muted">
            Procent liczony jest wśród osób, które odpowiedziały na dane pytanie. Przy pytaniach wielokrotnego wyboru suma procentów może przekraczać 100%.
          </p>
          <div v-if="statistics.questions.length" class="space-y-4">
            <article
              v-for="question in statistics.questions"
              :key="question.blockId"
              class="card md:p-5"
            >
              <div class="flex flex-wrap items-start justify-between gap-3">
                <div>
                  <p class="text-sm font-semibold text-paprika-600">{{ question.lessonTitle }}</p>
                  <h3 class="mt-1 break-words font-bold">{{ question.questionText }}</h3>
                </div>
                <span class="badge shrink-0">
                  {{ question.responseCount }} odp.
                </span>
              </div>

              <div v-if="question.options.length" class="mt-5 space-y-4">
                <div v-for="option in question.options" :key="option.optionId" class="space-y-1.5">
                  <div class="flex flex-wrap justify-between gap-2 text-sm">
                    <span class="font-medium">{{ option.optionText }}</span>
                    <span class="font-semibold">
                      {{ option.percentage }}% ({{ option.selectionCount }})
                    </span>
                  </div>
                  <div class="h-2 overflow-hidden rounded-full bg-silver/30" role="presentation">
                    <div
                      class="h-full rounded-full bg-spicy-paprika"
                      :style="{ width: `${Math.min(option.percentage, 100)}%` }"
                    ></div>
                  </div>
                  <p class="break-words text-sm text-muted">
                    Wybrali: {{ option.students.length ? option.students.join(', ') : 'nikt' }}
                  </p>
                </div>
              </div>

              <div v-if="question.textAnswers.length" class="mt-5 space-y-3">
                <h4 class="text-sm font-bold">Odpowiedzi tekstowe</h4>
                <div v-for="answer in question.textAnswers" :key="answer.answerText" class="rounded-lg bg-floral-white p-3">
                  <div class="flex flex-wrap justify-between gap-2 text-sm">
                    <span class="break-words font-medium">{{ answer.answerText }}</span>
                    <span class="shrink-0 font-semibold">
                      {{ answer.percentage }}% ({{ answer.selectionCount }})
                    </span>
                  </div>
                  <p class="mt-1 break-words text-sm text-muted">
                    Odpowiedzieli: {{ answer.students.join(', ') }}
                  </p>
                </div>
              </div>

              <p v-if="!question.responseCount" class="mt-4 text-sm text-muted">
                Nikt jeszcze nie odpowiedział na to pytanie.
              </p>
            </article>
          </div>
          <p v-else class="empty-state">
            Ten kurs nie zawiera pytań do statystyk.
          </p>
        </div>
      </div>

    </div>
  </SidebarLayout>
</template>
