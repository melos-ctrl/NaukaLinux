<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import RemoteDesktop from '../components/RemoteDesktop.vue';
import MarkdownContent from '@/components/MarkdownContent.vue';
import CourseReviews from '@/components/CourseReviews.vue';

interface QuizOption {
  id: number;
  optionText: string;
}

interface AnswerResult {
  optionIds: number[];
  textAnswer?: string;
  isCorrect: boolean;
}

interface SavedAnswerDto extends AnswerResult {
  blockId: number;
}

type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'TextAnswer';

interface BlockData {
  id?: number;
  type: number | string;
  orderIndex: number;
  textContent?: string;
  questionText?: string;
  questionType?: QuestionType;
  frontText?: string;
  backText?: string;
  videoUrl?: string;
  hint?: string;
  options?: QuizOption[];
}

interface LessonData {
  id?: number;
  title: string;
  orderIndex: number;
  blocks: BlockData[];
}

interface ModuleData {
  title: string;
  order: number;
  lessons: LessonData[];
}

interface SummaryQuestion {
  blockId: number;
  questionText: string;
  lessonTitle: string;
  lessonOrderIndex: number;
  status: 'wrong' | 'unanswered';
}

interface CourseSummary {
  courseTitle: string;
  isCompleted: boolean;
  totalLessons: number;
  completedLessons: number;
  questionCount: number;
  answeredCount: number;
  correctCount: number;
  score: number;
  maxScore: number;
  percentage: number;
  completedAt?: string | null;
  questionsToReview: SummaryQuestion[];
}

interface Task {
  id: number;
  orderIndex: number;
  moduleTitle?: string;
  lessonTitle: string;
  blocks: BlockData[];
}

const route = useRoute();
const router = useRouter();

const courseId = computed(() => Number(route.params.id));

const isLoading = ref(true);
const errorMessage = ref('');
const courseTitle = ref('');
const isVncOpen = ref(false);

const tasks = ref<Task[]>([]);
const currentStepIndex = ref(0);
const visibleHints = ref<Record<number, boolean>>({});
const answeredQuestions = ref<Record<number, AnswerResult | undefined>>({});
const checkingAnswers = ref<Record<number, boolean | undefined>>({});
const answerErrors = ref<Record<number, string | undefined>>({});
const selectedOptions = ref<Record<number, number[] | undefined>>({});
const textAnswers = ref<Record<number, string | undefined>>({});

// Podsumowanie kursu (ekran po kliknięciu "Zakończ kurs")
const summary = ref<CourseSummary | null>(null);
const isSummaryOpen = ref(false);
const isSummaryLoading = ref(false);
const summaryError = ref('');

// Modal do zgłaszania błędów w zadaniach i wysyłania prywatnych uwag do mentora
const isFeedbackModalOpen = ref(false);
const feedbackBlockId = ref<number | null>(null);
const feedbackMessage = ref('');
const feedbackSuccess = ref('');
const feedbackError = ref('');

// Modal z publicznymi ocenami kursu (gwiazdki + komentarz)
const isReviewsModalOpen = ref(false);

const currentTask = computed(() => tasks.value[currentStepIndex.value]);
const progressPercentage = computed(() =>
  tasks.value.length > 0 ? ((currentStepIndex.value + 1) / tasks.value.length) * 100 : 0
);

const getConfig = () => {
  const token = localStorage.getItem('token');
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {};
};

const fetchCourseContent = async () => {
  const courseId = route.params.id;

  if (!courseId) {
    errorMessage.value = "Brak ID kursu.";
    isLoading.value = false;
    return;
  }

  try {
    const config = getConfig();

    // Pobieramy strukturę kursu
    const response = await axios.get(`/api/courses/${courseId}/content`, config);
    const data = response.data;
    courseTitle.value = data.title;

    const parsedTasks: Task[] = [];
    const modules: ModuleData[] = ((data.modules || []) as ModuleData[])
      .slice()
      .sort((a: ModuleData, b: ModuleData) => a.order - b.order);
    const lessonsWithModules: { lesson: LessonData; moduleTitle?: string }[] = modules.length > 0
      ? modules.flatMap(module => module.lessons.map(lesson => ({ lesson, moduleTitle: module.title })))
      : ((data.lessons || []) as LessonData[]).map(lesson => ({ lesson, moduleTitle: undefined }));

    lessonsWithModules
      .sort((a, b) => a.lesson.orderIndex - b.lesson.orderIndex)
      .forEach(({ lesson, moduleTitle }, index) => {
        const sortedBlocks = [...lesson.blocks].sort((a, b) => a.orderIndex - b.orderIndex);
        parsedTasks.push({
          id: index + 1,
          orderIndex: lesson.orderIndex || index + 1,
          moduleTitle,
          lessonTitle: lesson.title,
          blocks: sortedBlocks
        });
      });

    tasks.value = parsedTasks;

    if (tasks.value.length === 0) {
      errorMessage.value = "Ten kurs nie ma jeszcze żadnych lekcji.";
      return;
    }

    // Sprawdzamy postęp użytkownika, aby wznowić od ostatniego ukończonego etapu
    try {
      const progRes = await axios.get(`/api/student/course/${courseId}`, config);
      const lastCompletedOrder = progRes.data.lastCompletedLessonOrderIndex || 0;
      (progRes.data.answers as SavedAnswerDto[] | undefined)?.forEach(answer => {
        answeredQuestions.value[answer.blockId] = {
          optionIds: answer.optionIds,
          textAnswer: answer.textAnswer,
          isCorrect: answer.isCorrect
        };
        selectedOptions.value[answer.blockId] = answer.optionIds;
        textAnswers.value[answer.blockId] = answer.textAnswer;
      });

      if (lastCompletedOrder > 0) {
        const nextIdx = tasks.value.findIndex(t => t.orderIndex > lastCompletedOrder);
        if (nextIdx !== -1) {
          currentStepIndex.value = nextIdx;
        } else {
          // Wszystkie lekcje ukończone - zamiast zaczynać od nowa pokazujemy podsumowanie
          currentStepIndex.value = 0;
          await openSummary();
        }
      }
    } catch {
      // Jeśli brak zapisanego postępu, zaczynamy od lekcji 0
    }

  } catch (error) {
    console.error(error);
    errorMessage.value = "Nie masz dostępu do tego kursu lub kurs nie istnieje.";
  } finally {
    isLoading.value = false;
  }
};

const toggleHint = (blockIndex: number) => {
  visibleHints.value[blockIndex] = !visibleHints.value[blockIndex];
};

// Zapis ukończonej lekcji. Punkty liczy backend na podstawie zapisanych odpowiedzi.
const saveCurrentStepProgress = async () => {
  const courseId = Number(route.params.id);
  if (!courseId || !currentTask.value) return;

  try {
    await axios.post('/api/student/progress', {
      courseId: courseId,
      completedLessonOrderIndex: currentTask.value.orderIndex
    }, getConfig());
  } catch (err) {
    console.error("Błąd zapisu postępu:", err);
  }
};

const scrollLessonToTop = () => {
  const lessonContainer = document.getElementById('lesson-scroll-container');
  if (lessonContainer) lessonContainer.scrollTop = 0;
};

const goToLesson = (index: number) => {
  currentStepIndex.value = index;
  // Podpowiedzi są zapamiętane po numerze bloku w lekcji, więc przy zmianie lekcji je chowamy.
  // Odpowiedzi zostają - są zapisane po ID pytania i mają się wyświetlać po powrocie do lekcji.
  visibleHints.value = {};
  scrollLessonToTop();
};

const fetchSummary = async () => {
  isSummaryLoading.value = true;
  summaryError.value = '';
  try {
    const response = await axios.get(`/api/student/course/${route.params.id}/summary`, getConfig());
    summary.value = response.data as CourseSummary;
  } catch (error) {
    console.error('Błąd pobierania podsumowania:', error);
    summaryError.value = 'Nie udało się pobrać podsumowania kursu.';
  } finally {
    isSummaryLoading.value = false;
  }
};

async function openSummary() {
  isSummaryOpen.value = true;
  await fetchSummary();
}

const nextStep = async () => {
  await saveCurrentStepProgress();

  if (currentStepIndex.value < tasks.value.length - 1) {
    goToLesson(currentStepIndex.value + 1);
  } else {
    await openSummary();
  }
};

// Z podsumowania: przejście do lekcji z pytaniem do poprawy
const reviewQuestion = (question: SummaryQuestion) => {
  const index = tasks.value.findIndex(task => task.orderIndex === question.lessonOrderIndex);
  isSummaryOpen.value = false;
  goToLesson(index !== -1 ? index : 0);
};

const reviewCourseFromStart = () => {
  isSummaryOpen.value = false;
  goToLesson(0);
};

const summaryHeadline = computed(() => {
  const percent = summary.value?.percentage ?? 0;
  if (percent === 100) return 'Perfekcyjnie! Wszystkie odpowiedzi poprawne.';
  if (percent >= 80) return 'Świetna robota!';
  if (percent >= 50) return 'Dobrze Ci idzie - popraw kilka pytań i będzie komplet.';
  return 'Warto wrócić do kilku zagadnień.';
});

const openFeedbackModal = (blockId: number | null = null) => {
  feedbackBlockId.value = blockId;
  feedbackMessage.value = '';
  feedbackSuccess.value = '';
  feedbackError.value = '';
  isFeedbackModalOpen.value = true;
};

const submitFeedback = async () => {
  if (!feedbackMessage.value.trim()) return;
  feedbackError.value = '';
  try {
    await axios.post('/api/student/feedback', {
      courseId: Number(route.params.id),
      lessonBlockId: feedbackBlockId.value,
      message: feedbackMessage.value
    }, getConfig());

    feedbackSuccess.value = 'Dziękujemy! Twoje zgłoszenie zostało wysłane.';
    setTimeout(() => {
      isFeedbackModalOpen.value = false;
    }, 1500);
  } catch (err) {
    console.error("Błąd wysyłania zgłoszenia:", err);
    feedbackError.value = axios.isAxiosError(err) && typeof err.response?.data === 'string'
      ? err.response.data
      : 'Nie udało się wysłać zgłoszenia.';
  }
};

const copyCommand = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text);
  } catch (err) {
    console.error(err);
  }
};

// Do iframe trafia wyłącznie adres embed YouTube. Każdy inny link (w tym "javascript:...") jest odrzucany.
const YOUTUBE_ID = /^[A-Za-z0-9_-]{11}$/;
const getEmbedUrl = (url?: string): string => {
  if (!url) return '';
  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'https:' && parsed.protocol !== 'http:') return '';

    const host = parsed.hostname.toLowerCase().replace(/^(www|m)\./, '');
    const parts = parsed.pathname.split('/').filter(Boolean);
    let videoId: string | null = null;

    if (host === 'youtu.be') {
      videoId = parts[0] ?? null;
    } else if (host === 'youtube.com' || host === 'youtube-nocookie.com') {
      if (parts[0] === 'watch') videoId = parsed.searchParams.get('v');
      else if (['embed', 'shorts', 'live', 'v'].includes(parts[0] ?? '')) videoId = parts[1] ?? null;
    }

    return videoId && YOUTUBE_ID.test(videoId) ? `https://www.youtube.com/embed/${videoId}` : '';
  } catch {
    return '';
  }
};

const toggleAnswerOption = (blockId: number, optionId: number, questionType?: QuestionType) => {
  const selected = selectedOptions.value[blockId] ?? [];
  selectedOptions.value[blockId] = questionType === 'TrueFalse'
    ? [optionId]
    : selected.includes(optionId)
      ? selected.filter(id => id !== optionId)
      : [...selected, optionId];
};

const checkAnswer = async (blockId: number, optionIds: number[] = [], textAnswer = '') => {
  if (answeredQuestions.value[blockId] || checkingAnswers.value[blockId]) return;
  if (optionIds.length === 0 && !textAnswer.trim()) return;

  checkingAnswers.value[blockId] = true;
  answerErrors.value[blockId] = undefined;

  try {
    const response = await axios.post(
      `/api/student/verify-block/${blockId}`,
      { optionIds, textAnswer },
      getConfig(),
    );
    answeredQuestions.value[blockId] = { optionIds, textAnswer, isCorrect: response.data.isCorrect };
  } catch (error) {
    console.error('Błąd sprawdzania odpowiedzi:', error);
    answerErrors.value[blockId] = 'Nie udało się sprawdzić odpowiedzi. Spróbuj ponownie.';
  } finally {
    checkingAnswers.value[blockId] = false;
  }
};

const retryQuestion = async (blockId: number) => {
  checkingAnswers.value[blockId] = true;
  answerErrors.value[blockId] = undefined;

  try {
    await axios.delete(`/api/student/answer/${blockId}`, getConfig());
    delete answeredQuestions.value[blockId];
    delete selectedOptions.value[blockId];
    delete textAnswers.value[blockId];
  } catch (error) {
    console.error('Błąd resetowania odpowiedzi:', error);
    answerErrors.value[blockId] = 'Nie udało się rozpocząć ponownej próby. Spróbuj ponownie.';
  } finally {
    checkingAnswers.value[blockId] = false;
  }
};

onMounted(() => {
  fetchCourseContent();
});
</script>

<template>
  <div class="fullscreen-view relative flex w-full bg-floral-white">

    <div
      :class="[
        isVncOpen ? 'w-1/3 min-w-[350px] max-w-[500px]' : 'flex-1',
        'z-10 flex h-full flex-col overflow-hidden bg-charcoal-brown text-floral-white transition-all duration-500 ease-in-out'
      ]"
    >
      <div class="mx-auto flex h-full w-full flex-col" :class="!isVncOpen ? 'max-w-4xl' : ''">

        <div v-if="isLoading" class="flex flex-1 items-center justify-center" role="status">
          <span class="animate-pulse text-floral-white">Ładowanie kursu...</span>
        </div>

        <div v-else-if="errorMessage" class="flex flex-1 flex-col items-center justify-center gap-4 p-6 text-center">
          <p class="text-spicy-paprika">{{ errorMessage }}</p>
          <button type="button" @click="router.push('/courses')" class="btn btn-light">
            Wróć do katalogu
          </button>
        </div>

        <!-- PODSUMOWANIE KURSU -->
        <div v-else-if="isSummaryOpen" class="flex-1 overflow-y-auto p-6 md:p-10">
          <div v-if="isSummaryLoading" class="flex h-full items-center justify-center" role="status">
            <span class="animate-pulse text-floral-white">Liczenie wyników...</span>
          </div>

          <div v-else-if="summaryError" class="flex h-full flex-col items-center justify-center gap-4 text-center">
            <p class="text-spicy-paprika">{{ summaryError }}</p>
            <button type="button" @click="fetchSummary" class="btn btn-light">
              Spróbuj ponownie
            </button>
          </div>

          <div v-else-if="summary" class="mx-auto flex max-w-2xl flex-col gap-6">
            <header class="text-center">
              <p class="text-xs font-semibold uppercase tracking-wider text-spicy-paprika">{{ summary.courseTitle }}</p>
              <h2 class="mt-2 font-mono text-3xl font-bold text-floral-white">
                {{ summary.isCompleted ? 'Kurs ukończony!' : 'Podsumowanie kursu' }}
              </h2>
              <p v-if="summary.questionCount > 0" class="mt-2 text-silver">{{ summaryHeadline }}</p>
            </header>

            <!-- Wynik -->
            <section v-if="summary.questionCount > 0" class="rounded-2xl border border-silver/30 bg-floral-white/5 p-6 text-center">
              <p class="font-mono text-5xl font-bold text-floral-white">
                {{ summary.score }}<span class="text-2xl text-silver"> / {{ summary.maxScore }} pkt</span>
              </p>
              <div
                class="mx-auto mt-4 h-3 max-w-md overflow-hidden rounded-full bg-floral-white/20"
                role="progressbar"
                :aria-valuenow="summary.percentage"
                aria-valuemin="0"
                aria-valuemax="100"
              >
                <div
                  class="h-full rounded-full transition-all duration-700"
                  :class="summary.percentage >= 80 ? 'bg-green-500' : 'bg-spicy-paprika'"
                  :style="{ width: summary.percentage + '%' }"
                ></div>
              </div>
              <p class="mt-2 text-sm font-semibold text-silver">{{ summary.percentage }}% poprawnych odpowiedzi</p>
            </section>

            <!-- Liczby -->
            <section class="grid grid-cols-3 gap-3 text-center">
              <div class="rounded-xl border border-silver/30 p-4">
                <p class="font-mono text-2xl font-bold text-floral-white">{{ summary.completedLessons }}/{{ summary.totalLessons }}</p>
                <p class="mt-1 text-xs uppercase tracking-wider text-silver">Lekcje</p>
              </div>
              <div class="rounded-xl border border-silver/30 p-4">
                <p class="font-mono text-2xl font-bold text-green-400">{{ summary.correctCount }}/{{ summary.questionCount }}</p>
                <p class="mt-1 text-xs uppercase tracking-wider text-silver">Poprawne</p>
              </div>
              <div class="rounded-xl border border-silver/30 p-4">
                <p class="font-mono text-2xl font-bold" :class="summary.questionsToReview.length ? 'text-spicy-paprika' : 'text-floral-white'">
                  {{ summary.questionsToReview.length }}
                </p>
                <p class="mt-1 text-xs uppercase tracking-wider text-silver">Do poprawy</p>
              </div>
            </section>

            <!-- Pytania do poprawy -->
            <section v-if="summary.questionsToReview.length" class="flex flex-col gap-2">
              <h3 class="text-sm font-bold uppercase tracking-wider text-silver">Wróć do tych pytań</h3>
              <button
                v-for="question in summary.questionsToReview"
                :key="question.blockId"
                type="button"
                @click="reviewQuestion(question)"
                class="flex items-start justify-between gap-3 rounded-xl border border-silver/30 p-4 text-left transition-colors hover:border-spicy-paprika hover:bg-floral-white/5"
              >
                <span>
                  <span class="block text-xs font-semibold text-silver">{{ question.lessonTitle }}</span>
                  <span class="mt-1 block font-medium text-floral-white">{{ question.questionText }}</span>
                </span>
                <span
                  class="shrink-0 rounded-full px-2.5 py-1 text-xs font-semibold"
                  :class="question.status === 'wrong' ? 'bg-red-500/20 text-red-300' : 'bg-silver/20 text-silver'"
                >
                  {{ question.status === 'wrong' ? 'Błędna' : 'Bez odpowiedzi' }}
                </span>
              </button>
            </section>
            <p v-else-if="summary.questionCount > 0" class="rounded-xl border border-green-500/40 bg-green-500/10 p-4 text-center text-sm font-semibold text-green-300">
              Wszystkie pytania rozwiązane poprawnie. Brawo!
            </p>

            <!-- Ocena kursu (gwiazdki + komentarz) -->
            <section class="flex flex-col gap-3 border-t border-silver/30 pt-6">
              <h3 class="text-xl font-bold text-floral-white">★ Oceń kurs</h3>
              <p class="-mt-2 text-sm text-silver">Twoja ocena pomoże innym wybrać kurs, a autorowi go ulepszyć.</p>
              <CourseReviews :course-id="courseId" />
            </section>

            <!-- Akcje -->
            <section class="flex flex-col gap-3 sm:flex-row">
              <button type="button" @click="openFeedbackModal(null)" class="btn btn-outline-dark flex-1">
                ✉ Uwagi dla mentora
              </button>
              <button type="button" @click="reviewCourseFromStart" class="btn btn-outline-dark flex-1">
                Przejrzyj kurs od początku
              </button>
              <button type="button" @click="router.push('/courses')" class="btn btn-light flex-1">
                Wróć do katalogu
              </button>
            </section>
          </div>
        </div>

        <template v-else-if="currentTask">

          <div class="flex flex-col gap-4 border-b border-silver/30 p-6">
            <div class="flex items-start justify-between gap-2">
              <div>
                <div class="mb-1 text-xs font-semibold uppercase tracking-wider text-spicy-paprika">
                  {{ courseTitle }}
                </div>
                <div v-if="currentTask.moduleTitle" class="text-xs font-semibold uppercase text-silver">{{ currentTask.moduleTitle }}</div>
                <h2 class="line-clamp-2 text-xl font-bold text-floral-white">Lekcja {{ currentStepIndex + 1 }}: {{ currentTask.lessonTitle }}</h2>
              </div>
              <div class="flex shrink-0 flex-col gap-2">
                <button type="button" @click="isReviewsModalOpen = true" class="btn btn-outline-dark btn-sm">
                  ★ Oceń kurs
                </button>
                <button type="button" @click="openFeedbackModal(null)" class="btn btn-outline-dark btn-sm">
                  ✉ Uwagi dla mentora
                </button>
                <button type="button" @click="openSummary" class="btn btn-outline-dark btn-sm">
                  Podsumowanie
                </button>
              </div>
            </div>

            <div>
              <div class="mb-2 flex justify-between text-xs font-semibold uppercase tracking-wider text-floral-white">
                <span>Postęp kursu</span>
                <span class="font-mono">{{ Math.round(progressPercentage) }}%</span>
              </div>
              <div class="h-2 w-full overflow-hidden rounded-full bg-floral-white/20">
                <div class="h-2 rounded-full bg-spicy-paprika transition-all duration-500 ease-in-out" :style="{ width: progressPercentage + '%' }"></div>
              </div>
            </div>
          </div>

          <div id="lesson-scroll-container" class="flex flex-1 flex-col gap-8 overflow-y-auto p-6">

            <div v-for="(block, index) in currentTask.blocks" :key="index" class="flex flex-col gap-2">

              <div v-if="getEmbedUrl(block.videoUrl)" class="mb-2 aspect-video w-full overflow-hidden rounded-xl border border-silver/30 bg-black shadow-md">
                <iframe
                  :src="getEmbedUrl(block.videoUrl)"
                  class="h-full w-full"
                  frameborder="0"
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowfullscreen>
                </iframe>
              </div>

              <MarkdownContent
                v-if="block.textContent"
                :source="block.textContent"
                dark
                :class="isVncOpen ? 'text-sm' : 'text-base'"
              />

              <div v-if="block.frontText" class="mt-4">
                <p class="mb-2 text-xs font-semibold uppercase tracking-wider text-silver">Polecenie do wpisania w terminalu:</p>
                <button
                  type="button"
                  @click="copyCommand(block.frontText)"
                  class="group relative w-full cursor-pointer rounded-xl border border-silver/40 bg-carbon-black p-4 text-left font-mono text-sm text-green-400 shadow-inner transition-colors hover:border-spicy-paprika"
                >
                  $ {{ block.frontText }}
                  <span class="absolute right-3 top-1/2 -translate-y-1/2 rounded bg-charcoal-brown px-2 py-1 font-sans text-xs text-floral-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100">
                    Kopiuj
                  </span>
                </button>
              </div>

              <div v-if="block.questionText" class="mt-4 rounded-2xl border border-silver/30 bg-floral-white/5 p-5">
                <div class="mb-4 flex items-start justify-between gap-4">
                  <h3 class="text-lg font-bold text-floral-white">{{ block.questionText }}</h3>
                  <div class="flex shrink-0 gap-2">
                    <button
                      v-if="block.hint"
                      type="button"
                      @click="toggleHint(index)"
                      class="btn btn-outline-dark btn-sm"
                    >
                      {{ visibleHints[index] ? 'Ukryj podpowiedź' : '? Podpowiedź' }}
                    </button>
                    <button
                      v-if="block.id !== undefined"
                      type="button"
                      @click="openFeedbackModal(block.id)"
                      class="btn btn-outline-dark btn-sm"
                      title="Zgłoś błąd w tym zadaniu"
                    >
                      ! Zgłoś błąd
                    </button>
                  </div>
                </div>

                <!-- Wyświetlanie podpowiedzi -->
                <div v-if="visibleHints[index] && block.hint" class="mb-4 rounded-lg border border-sky-blue/30 bg-sky-blue/10 p-3 text-sm text-floral-white">
                  <strong class="text-sky-blue">Podpowiedź:</strong> {{ block.hint }}
                </div>

                <div v-if="block.questionType === 'TextAnswer'" class="flex flex-col gap-3">
                  <input
                    v-if="block.id !== undefined"
                    v-model="textAnswers[block.id]"
                    type="text"
                    :disabled="!!answeredQuestions[block.id] || !!checkingAnswers[block.id]"
                    @keydown.enter="checkAnswer(block.id, [], textAnswers[block.id] ?? '')"
                    placeholder="Wpisz odpowiedź..."
                    aria-label="Twoja odpowiedź"
                    class="input input-dark font-mono"
                  />
                  <button
                    v-if="block.id !== undefined"
                    type="button"
                    @click="checkAnswer(block.id, [], textAnswers[block.id] ?? '')"
                    :disabled="!!answeredQuestions[block.id] || !!checkingAnswers[block.id] || !textAnswers[block.id]?.trim()"
                    class="btn btn-primary self-start"
                  >Sprawdź odpowiedź</button>
                  <div v-if="block.id !== undefined && checkingAnswers[block.id]" class="text-sm text-silver">Sprawdzanie odpowiedzi...</div>
                  <div v-if="block.id !== undefined && answerErrors[block.id]" class="text-sm text-red-300">{{ answerErrors[block.id] }}</div>
                  <div v-if="block.id !== undefined && answeredQuestions[block.id]" class="text-center text-sm font-bold">
                    <span v-if="answeredQuestions[block.id]?.isCorrect" class="text-green-400">Świetnie! Poprawna odpowiedź (+10 pkt).</span>
                    <span v-else class="text-red-300">Niestety, to nie jest poprawna odpowiedź.</span>
                  </div>
                  <button
                    v-if="block.id !== undefined && answeredQuestions[block.id] && !answeredQuestions[block.id]?.isCorrect"
                    type="button"
                    @click="retryQuestion(block.id)"
                    :disabled="!!checkingAnswers[block.id]"
                    class="btn btn-outline-dark self-start"
                  >Spróbuj ponownie</button>
                </div>

                <div v-else-if="block.options && block.options.length > 0" class="flex flex-col gap-3">
                  <button
                    v-for="(option, idx) in block.options"
                    :key="idx"
                    type="button"
                    @click="block.id !== undefined && toggleAnswerOption(block.id, option.id, block.questionType)"
                    :disabled="block.id === undefined || !!answeredQuestions[block.id] || !!checkingAnswers[block.id]"
                    :aria-pressed="block.id !== undefined && (selectedOptions[block.id] ?? []).includes(option.id)"
                    :class="[
                      'rounded-lg border px-4 py-3 text-left text-sm shadow-sm transition-colors',
                      block.id !== undefined && !answeredQuestions[block.id] && !checkingAnswers[block.id] && !(selectedOptions[block.id] ?? []).includes(option.id) ? 'cursor-pointer border-silver/40 bg-charcoal-brown text-floral-white hover:border-spicy-paprika hover:bg-white/5' : '',
                      block.id !== undefined && !answeredQuestions[block.id] && (selectedOptions[block.id] ?? []).includes(option.id) ? 'border-paprika-600 bg-paprika-600 text-white' : '',
                      block.id !== undefined && answeredQuestions[block.id]?.optionIds.includes(option.id) && answeredQuestions[block.id]?.isCorrect ? 'cursor-default border-green-500 bg-green-700 text-white' : '',
                      block.id !== undefined && answeredQuestions[block.id]?.optionIds.includes(option.id) && !answeredQuestions[block.id]?.isCorrect ? 'cursor-default border-red-500 bg-red-700 text-white' : '',
                      block.id !== undefined && answeredQuestions[block.id] && !answeredQuestions[block.id]?.optionIds.includes(option.id) ? 'cursor-not-allowed border-silver/20 bg-charcoal-brown text-silver opacity-50' : ''
                    ]"
                  >
                    {{ option.optionText }}
                  </button>

                  <button
                    v-if="block.id !== undefined"
                    type="button"
                    @click="checkAnswer(block.id, selectedOptions[block.id] ?? [], textAnswers[block.id] ?? '')"
                    :disabled="!!answeredQuestions[block.id] || !!checkingAnswers[block.id] || !(selectedOptions[block.id]?.length)"
                    class="btn btn-primary self-start"
                  >Sprawdź odpowiedź</button>

                  <div v-if="block.id !== undefined && checkingAnswers[block.id]" class="mt-3 text-center text-sm text-silver">
                    Sprawdzanie odpowiedzi...
                  </div>
                  <div v-if="block.id !== undefined && answerErrors[block.id]" class="mt-3 text-center text-sm text-red-300">
                    {{ answerErrors[block.id] }}
                  </div>
                  <div v-if="block.id !== undefined && answeredQuestions[block.id]" class="mt-3 text-center text-sm font-bold">
                    <span v-if="answeredQuestions[block.id]?.isCorrect" class="text-green-400">
                      Świetnie! Poprawna odpowiedź (+10 pkt).
                    </span>
                    <span v-else class="text-red-300">
                      Niestety, to nie jest poprawna odpowiedź.
                    </span>
                  </div>
                  <button
                    v-if="block.id !== undefined && answeredQuestions[block.id] && !answeredQuestions[block.id]?.isCorrect"
                    type="button"
                    @click="retryQuestion(block.id)"
                    :disabled="!!checkingAnswers[block.id]"
                    class="btn btn-outline-dark self-start"
                  >Spróbuj ponownie</button>
                </div>

              </div>

            </div>
          </div>

          <div class="shrink-0 border-t border-silver/30 bg-charcoal-brown p-6">
            <button type="button" @click="nextStep" class="btn btn-light w-full">
              {{ currentStepIndex === tasks.length - 1 ? 'Zakończ kurs' : 'Przejdź do następnej lekcji' }}
            </button>
          </div>
        </template>
      </div>
    </div>

    <div
      :class="[
        isVncOpen ? 'flex-1 border-l border-silver' : 'w-0 border-l-0',
        'relative flex flex-col items-center justify-center bg-floral-white transition-all duration-500 ease-in-out'
      ]"
    >
      <button
        type="button"
        @click="isVncOpen = !isVncOpen"
        :aria-label="isVncOpen ? 'Zamknij terminal' : 'Otwórz terminal'"
        class="absolute -left-8 top-1/2 z-50 flex h-20 w-8 -translate-y-1/2 cursor-pointer items-center justify-center rounded-l-xl border border-r-0 border-line bg-floral-white text-charcoal-brown shadow-lg transition-colors hover:bg-paprika-600 hover:text-white"
      >
        <span v-if="!isVncOpen" class="text-xl font-bold" aria-hidden="true">&#9664;</span>
        <span v-else class="text-xl font-bold" aria-hidden="true">&#9654;</span>
      </button>

      <div class="relative h-full w-full overflow-hidden">
        <RemoteDesktop />
        <div v-show="isVncOpen" class="pointer-events-none absolute right-4 top-4 z-10 rounded-lg border border-charcoal-brown bg-black/60 px-3 py-1 text-xs text-floral-white backdrop-blur transition-opacity">
          Połączenie aktywne
        </div>
      </div>
    </div>

    <!-- MODAL Z OCENAMI KURSU -->
    <div
      v-if="isReviewsModalOpen"
      class="fixed inset-0 z-[200] flex items-center justify-center bg-black/60 p-4"
      @click.self="isReviewsModalOpen = false"
    >
      <div
        class="flex max-h-[85vh] w-full max-w-2xl flex-col rounded-2xl border border-silver/40 bg-charcoal-brown shadow-xl"
        role="dialog"
        aria-modal="true"
        aria-labelledby="reviews-title"
      >
        <div class="flex items-start justify-between gap-4 border-b border-silver/30 p-5">
          <div class="min-w-0">
            <p class="text-xs font-semibold uppercase tracking-wider text-spicy-paprika">Oceń kurs</p>
            <h3 id="reviews-title" class="truncate text-xl font-bold text-floral-white">{{ courseTitle }}</h3>
          </div>
          <button
            type="button"
            @click="isReviewsModalOpen = false"
            class="shrink-0 rounded-lg px-2 text-2xl leading-none text-silver hover:text-floral-white"
            aria-label="Zamknij"
          >×</button>
        </div>
        <div class="overflow-y-auto p-5">
          <CourseReviews :course-id="courseId" />
        </div>
      </div>
    </div>

    <!-- MODAL ZGŁASZANIA BŁĘDU / UWAG DLA MENTORA -->
    <div v-if="isFeedbackModalOpen" class="fixed inset-0 z-[200] flex items-center justify-center bg-black/60 p-4">
      <div class="w-full max-w-md rounded-2xl border border-silver/40 bg-charcoal-brown p-6 text-floral-white shadow-xl" role="dialog" aria-modal="true" aria-labelledby="feedback-title">
        <h3 id="feedback-title" class="mb-2 text-xl font-bold">
          {{ feedbackBlockId ? 'Zgłoś błąd w zadaniu' : 'Uwagi dla mentora' }}
        </h3>
        <p class="mb-4 text-sm text-silver">
          {{ feedbackBlockId ? 'Opisz, co nie działa lub jest niejasne w tym pytaniu.' : 'Tę wiadomość zobaczy tylko autor kursu. Publiczną ocenę wystawisz przyciskiem „★ Oceń kurs”.' }}
        </p>

        <textarea
          v-model="feedbackMessage"
          rows="4"
          maxlength="2000"
          placeholder="Twoja wiadomość..."
          aria-label="Treść wiadomości"
          class="input input-dark mb-4"
        ></textarea>

        <p v-if="feedbackSuccess" class="mb-4 text-sm font-bold text-green-400" role="status">{{ feedbackSuccess }}</p>
        <p v-if="feedbackError" class="mb-4 text-sm font-bold text-red-300" role="alert">{{ feedbackError }}</p>

        <div class="flex justify-end gap-3">
          <button type="button" @click="isFeedbackModalOpen = false" class="btn btn-outline-dark">
            Anuluj
          </button>
          <button type="button" @click="submitFeedback" class="btn btn-primary">
            Wyślij
          </button>
        </div>
      </div>
    </div>

  </div>
</template>
