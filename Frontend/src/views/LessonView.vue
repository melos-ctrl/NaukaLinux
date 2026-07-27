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
  type: number | string;
  orderIndex: number;
  textContent?: string;
  questionText?: string;
  frontText?: string;
  backText?: string;
  videoUrl?: string;
  options?: QuizOption[];
}

interface LessonData {
  title: string;
  orderIndex: number;
  blocks: BlockData[];
}

interface Task {
  id: number;
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
const showHint = ref(false);
const answeredQuestions = ref<Record<number, QuizOption | null>>({});

const currentTask = computed(() => tasks.value[currentStepIndex.value]);
const progressPercentage = computed(() => 
  tasks.value.length > 0 ? ((currentStepIndex.value + 1) / tasks.value.length) * 100 : 0
);

const fetchCourseContent = async () => {
  const courseId = route.params.id;
  
  if (!courseId) {
    errorMessage.value = "Brak ID kursu.";
    isLoading.value = false;
    return;
  }

  try {
    const token = localStorage.getItem('token');
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {};

    const response = await axios.get(`http://localhost:5042/api/courses/${courseId}/content`, config);
    const data = response.data;
    
    courseTitle.value = data.title;

    const parsedTasks: Task[] = [];
    let stepCounter = 1;

    data.lessons.forEach((lesson: LessonData) => {
      const sortedBlocks = lesson.blocks.sort((a, b) => a.orderIndex - b.orderIndex);
      
      parsedTasks.push({
        id: stepCounter++,
        lessonTitle: lesson.title,
        blocks: sortedBlocks
      });
    });

    tasks.value = parsedTasks;
    
    if (tasks.value.length === 0) {
      errorMessage.value = "Ten kurs nie ma jeszcze żadnych lekcji.";
    }

  } catch (error) {
    console.error(error);
    errorMessage.value = "Nie masz dostępu do tego kursu lub kurs nie istnieje.";
  } finally {
    isLoading.value = false;
  }
};

const nextStep = () => {
  answeredQuestions.value = {};
  if (currentStepIndex.value < tasks.value.length - 1) {
    currentStepIndex.value++;
    showHint.value = false;
    
    const lessonContainer = document.getElementById('lesson-scroll-container');
    if (lessonContainer) lessonContainer.scrollTop = 0;

  } else {
    alert('Gratulacje! Ukończyłeś cały kurs!');
    router.push('/courses');
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
            <div>
              <div class="text-xs text-spicy-paprika font-bold uppercase tracking-wider mb-1">
                {{ courseTitle }}
              </div>
              <h2 class="text-xl font-bold text-floral-white line-clamp-2">Lekcja {{ currentStepIndex + 1 }}: {{ currentTask.lessonTitle }}</h2>
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
                <h3 class="text-lg font-bold text-floral-white mb-4">{{ block.questionText }}</h3>
                
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
                    <span v-if="answeredQuestions[index].isCorrect" class="text-green-400">
                      Świetnie! Poprawna odpowiedź.
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

  </div>
</template>