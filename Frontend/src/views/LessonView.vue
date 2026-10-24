<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import axios from 'axios';
import RemoteDesktop from '../components/RemoteDesktop.vue';

interface QuizOption {
  optionText: string;
  isCorrect: boolean;
}

interface BlockData {
  id?: number;
  type: number | string;
  orderIndex: number;
  textContent?: string;
  questionText?: string;
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

interface Task {
  id: number;
  orderIndex: number;
  lessonTitle: string;
  blocks: BlockData[];
}

const route = useRoute();
const router = useRouter();

const isLoading = ref(true);
const errorMessage = ref('');
const courseTitle = ref('');
const isVncOpen = ref(false);

const tasks = ref<Task[]>([]);
const currentStepIndex = ref(0);
const visibleHints = ref<Record<number, boolean>>({});
const answeredQuestions = ref<Record<number, QuizOption | null>>({});

// Modal do zgłaszania błędów w zadaniach i wysyłania opinii
const isFeedbackModalOpen = ref(false);
const feedbackBlockId = ref<number | null>(null);
const feedbackMessage = ref('');
const feedbackSuccess = ref('');

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
    let stepCounter = 1;

    const sortedLessons = (data.lessons || []).sort((a: LessonData, b: LessonData) => a.orderIndex - b.orderIndex);

    sortedLessons.forEach((lesson: LessonData) => {
      const sortedBlocks = lesson.blocks.sort((a, b) => a.orderIndex - b.orderIndex);
      parsedTasks.push({
        id: stepCounter++,
        orderIndex: lesson.orderIndex || stepCounter - 1,
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
      if (lastCompletedOrder > 0) {
        const nextIdx = tasks.value.findIndex(t => t.orderIndex > lastCompletedOrder);
        if (nextIdx !== -1) {
          currentStepIndex.value = nextIdx;
        } else {
          currentStepIndex.value = tasks.value.length - 1;
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

// Obliczanie punktów zdobytych w bieżącej lekcji i zapis do bazy
const saveCurrentStepProgress = async () => {
  const courseId = Number(route.params.id);
  if (!courseId || !currentTask.value) return;

  let earnedPoints = 0;
  Object.values(answeredQuestions.value).forEach((ans) => {
    if (ans && ans.isCorrect) earnedPoints += 10;
  });

  try {
    await axios.post('/api/student/progress', {
      courseId: courseId,
      completedLessonOrderIndex: currentTask.value.orderIndex,
      score: earnedPoints
    }, getConfig());
  } catch (err) {
    console.error("Błąd zapisu postępu:", err);
  }
};

const nextStep = async () => {
  await saveCurrentStepProgress();

  answeredQuestions.value = {};
  visibleHints.value = {};

  if (currentStepIndex.value < tasks.value.length - 1) {
    currentStepIndex.value++;
    
    const lessonContainer = document.getElementById('lesson-scroll-container');
    if (lessonContainer) lessonContainer.scrollTop = 0;
  } else {
    alert('Gratulacje! Ukończyłeś cały kurs!');
    router.push('/courses');
  }
};

const openFeedbackModal = (blockId: number | null = null) => {
  feedbackBlockId.value = blockId;
  feedbackMessage.value = '';
  feedbackSuccess.value = '';
  isFeedbackModalOpen.value = true;
};

const submitFeedback = async () => {
  if (!feedbackMessage.value.trim()) return;
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
  }
};

const copyCommand = async (text: string) => {
  try {
    await navigator.clipboard.writeText(text);
  } catch (err) {
    console.error(err);
  }
};

const getEmbedUrl = (url: string) => {
  if (!url) return '';
  if (url.includes('/embed/')) return url;
  const videoIdMatch = url.match(/(?:v=|\/)([0-9A-Za-z_-]{11}).*/);
  if (videoIdMatch && videoIdMatch[1]) {
    return `https://www.youtube.com/embed/${videoIdMatch[1]}`;
  }
  return url; 
};

const checkAnswer = (blockIndex: number, option: QuizOption) => {
  if (answeredQuestions.value[blockIndex]) return;
  answeredQuestions.value[blockIndex] = option;
};

onMounted(() => {
  fetchCourseContent();
});
</script>

<template>
  <div class="flex h-screen w-full bg-floral-white overflow-hidden relative">

    <div 
      :class="[
        isVncOpen ? 'w-1/3 min-w-[350px] max-w-[500px]' : 'flex-1',
        'bg-charcoal-brown text-white flex flex-col z-10 transition-all duration-500 ease-in-out h-full overflow-hidden'
      ]"
    >
      <div class="w-full h-full mx-auto flex flex-col" :class="!isVncOpen ? 'max-w-4xl' : ''">
        
        <div v-if="isLoading" class="flex-1 flex justify-center items-center">
          <span class="animate-pulse text-floral-white">Ładowanie kursu</span>
        </div>

        <div v-else-if="errorMessage" class="flex-1 flex flex-col justify-center items-center p-6 text-center">
          <p class="text-spicy-paprika mb-4">{{ errorMessage }}</p>
          <button @click="router.push('/courses')" class="px-4 py-2 bg-floral-white text-charcoal-brown hover:bg-spicy-paprika hover:text-floral-white rounded transition-colors">
            Wróć do katalogu
          </button>
        </div>

        <template v-else-if="currentTask">
          
          <div class="p-6 border-b border-silver flex flex-col gap-4">
            <div class="flex justify-between items-start gap-2">
              <div>
                <div class="text-xs text-spicy-paprika font-bold uppercase tracking-wider mb-1">
                  {{ courseTitle }}
                </div>
                <h2 class="text-xl font-bold text-floral-white line-clamp-2">Lekcja {{ currentStepIndex + 1 }}: {{ currentTask.lessonTitle }}</h2>
              </div>
              <button 
                @click="openFeedbackModal(null)"
                class="shrink-0 text-xs px-3 py-1.5 rounded-lg border border-silver/40 text-silver hover:border-spicy-paprika hover:text-floral-white transition-colors"
              >
                ★ Oceń / Wyślij opinię
              </button>
            </div>
            
            <div>
              <div class="flex justify-between text-xs font-bold text-floral-white uppercase tracking-wider mb-2">
                <span>Postęp kursu</span>
                <span>{{ Math.round(progressPercentage) }}%</span>
              </div>
              <div class="w-full bg-floral-white/20 rounded-full h-2 overflow-hidden">
                <div class="bg-spicy-paprika h-2 rounded-full transition-all duration-500 ease-in-out" :style="{ width: progressPercentage + '%' }"></div>
              </div>
            </div>
          </div>

          <div id="lesson-scroll-container" class="flex-1 p-6 overflow-y-auto flex flex-col gap-8">
            
            <div v-for="(block, index) in currentTask.blocks" :key="index" class="flex flex-col gap-2">
              
              <div v-if="block.videoUrl" class="w-full aspect-video bg-black rounded-lg overflow-hidden shadow-md border border-silver/30 mb-2">
                <iframe 
                  :src="getEmbedUrl(block.videoUrl)" 
                  class="w-full h-full" 
                  frameborder="0" 
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture" 
                  allowfullscreen>
                </iframe>
              </div>

              <p v-if="block.textContent" :class="['text-floral-white leading-relaxed', isVncOpen ? 'text-sm' : 'text-base']">
                {{ block.textContent }}
              </p>

              <div v-if="block.frontText" class="mt-4">
                <label class="block text-xs font-bold text-silver uppercase tracking-wider mb-2">Polecenie do wpisania w terminalu:</label>
                <div 
                  @click="copyCommand(block.frontText)"
                  class="group relative p-4 bg-slate-950 rounded-lg border border-silver/50 font-mono text-sm text-green-400 w-full cursor-pointer hover:border-spicy-paprika transition-colors shadow-inner"
                >
                  $ {{ block.frontText }}
                  <span class="absolute right-3 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 text-xs bg-charcoal-brown px-2 py-1 rounded text-floral-white transition-opacity">
                    Kopiuj
                  </span>
                </div>
              </div>

              <div v-if="block.questionText" class="bg-silver/10 p-5 rounded-xl border border-silver/30 mt-4">
                <div class="flex justify-between items-start gap-4 mb-4">
                  <h3 class="text-lg font-bold text-floral-white">{{ block.questionText }}</h3>
                  <div class="flex gap-2 shrink-0">
                    <button 
                      v-if="block.hint"
                      @click="toggleHint(index)"
                      class="text-xs px-2.5 py-1 rounded bg-sky-blue/20 text-sky-blue hover:bg-sky-blue/30 font-semibold transition-colors"
                    >
                      {{ visibleHints[index] ? 'Ukryj podpowiedź' : '? Podpowiedź' }}
                    </button>
                    <button 
                      @click="openFeedbackModal(block.id || index + 1)"
                      class="text-xs px-2.5 py-1 rounded bg-spicy-paprika/20 text-spicy-paprika hover:bg-spicy-paprika hover:text-white font-semibold transition-colors"
                      title="Zgłoś błąd w tym zadaniu"
                    >
                      ! Zgłoś błąd
                    </button>
                  </div>
                </div>

                <!-- Wyświetlanie podpowiedzi -->
                <div v-if="visibleHints[index] && block.hint" class="mb-4 p-3 rounded-lg bg-sky-blue/10 border border-sky-blue/30 text-sm text-floral-white">
                  <strong class="text-sky-blue">Podpowiedź:</strong> {{ block.hint }}
                </div>
                
                <div v-if="block.options && block.options.length > 0" class="flex flex-col gap-3">
                  <button 
                    v-for="(option, idx) in block.options" 
                    :key="idx"
                    @click="checkAnswer(index, option)"
                    :disabled="!!answeredQuestions[index]"
                    :class="[
                      'px-4 py-3 border rounded-lg text-left transition-colors text-sm shadow-sm',
                      !answeredQuestions[index] ? 'bg-charcoal-brown border-silver/50 hover:bg-spicy-paprika hover:border-spicy-paprika hover:text-floral-white text-white cursor-pointer' : '',
                      answeredQuestions[index] === option && option.isCorrect ? 'bg-green-600/90 border-green-500 text-white cursor-default' : '',
                      answeredQuestions[index] === option && !option.isCorrect ? 'bg-red-600/90 border-red-500 text-white cursor-default' : '',
                      answeredQuestions[index] && answeredQuestions[index] !== option && option.isCorrect ? 'bg-green-900/50 border-green-500/50 text-green-400 cursor-default' : '',
                      answeredQuestions[index] && answeredQuestions[index] !== option && !option.isCorrect ? 'bg-charcoal-brown border-silver/20 text-gray-500 opacity-50 cursor-not-allowed' : ''
                    ]"
                  >
                    {{ option.optionText }}
                  </button>
                  
                  <div v-if="answeredQuestions[index]" class="mt-3 font-bold text-sm text-center">
                    <span v-if="answeredQuestions[index]?.isCorrect" class="text-green-400">
                      Świetnie! Poprawna odpowiedź (+10 pkt).
                    </span>
                    <span v-else class="text-red-400">
                      Niestety to zła odpowiedź. Zobacz poprawną powyżej.
                    </span>
                  </div>
                </div>
              </div>

            </div>
          </div>

          <div class="p-6 border-t border-silver bg-charcoal-brown shrink-0">
            <button 
              @click="nextStep"
              class="w-full px-6 py-3 bg-floral-white hover:bg-spicy-paprika text-charcoal-brown hover:text-floral-white font-semibold rounded-lg shadow-md transition-colors flex justify-center items-center gap-2"
            >
              {{ currentStepIndex === tasks.length - 1 ? 'Zakończ kurs' : 'Przejdź do następnej lekcji' }}
            </button>
          </div>
        </template>
      </div>
    </div>

    <div 
      :class="[
        isVncOpen ? 'flex-1 border-l border-silver' : 'w-0 border-l-0',
        'relative bg-floral-white flex flex-col items-center justify-center transition-all duration-500 ease-in-out'
      ]"
    >
      <button 
        @click="isVncOpen = !isVncOpen"
        class="absolute top-1/2 -translate-y-1/2 -left-8 w-8 h-20 bg-floral-white hover:bg-spicy-paprika text-charcoal-brown hover:text-floral-white rounded-l-xl shadow-lg flex items-center justify-center z-50 transition-colors cursor-pointer border border-r-0 border-silver/50"
      >
        <span v-if="!isVncOpen" class="text-xl font-bold">&#9664;</span>
        <span v-else class="text-xl font-bold">&#9654;</span>
      </button>

      <div class="w-full h-full overflow-hidden relative">
        <RemoteDesktop />
        <div v-show="isVncOpen" class="absolute top-4 right-4 bg-black/60 backdrop-blur px-3 py-1 rounded text-xs text-floral-white border border-charcoal-brown pointer-events-none z-10 transition-opacity">
          Połączenie aktywne
        </div>
      </div>
    </div>

    <!-- MODAL ZGŁASZANIA BŁĘDU / OPINII -->
    <div v-if="isFeedbackModalOpen" class="fixed inset-0 bg-black/60 z-50 flex items-center justify-center p-4">
      <div class="bg-charcoal-brown border border-silver/40 text-floral-white rounded-2xl max-w-md w-full p-6 shadow-xl">
        <h3 class="text-xl font-bold mb-2">
          {{ feedbackBlockId ? 'Zgłoś błąd w zadaniu' : 'Wyślij opinię o kursie' }}
        </h3>
        <p class="text-sm text-silver mb-4">
          {{ feedbackBlockId ? 'Opisz, co nie działa lub jest niejasne w tym pytaniu.' : 'Podziel się swoją opinią na temat materiałów w tym kursie.' }}
        </p>

        <textarea 
          v-model="feedbackMessage"
          rows="4"
          placeholder="Twoja wiadomość..."
          class="w-full rounded-lg border border-silver/50 bg-carbon-black p-3 text-floral-white placeholder:text-silver/50 focus:outline-none focus:ring-2 focus:ring-spicy-paprika mb-4"
        ></textarea>

        <p v-if="feedbackSuccess" class="text-green-400 text-sm font-bold mb-4">{{ feedbackSuccess }}</p>

        <div class="flex justify-end gap-3">
          <button 
            @click="isFeedbackModalOpen = false"
            class="px-4 py-2 rounded-lg border border-silver/40 text-silver hover:text-white"
          >
            Anuluj
          </button>
          <button 
            @click="submitFeedback"
            class="px-5 py-2 rounded-lg bg-spicy-paprika text-white font-semibold hover:opacity-90"
          >
            Wyślij
          </button>
        </div>
      </div>
    </div>

  </div>
</template>