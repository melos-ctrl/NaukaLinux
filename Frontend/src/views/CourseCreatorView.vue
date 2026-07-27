<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'

const route = useRoute()
const router = useRouter()

const course = ref({
  id: null,
  title: '',
  description: '',
  modules: []
})

const isEditing = ref(false)
const activeElement = ref(null)
const isSaving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const selectElement = (element) => {
  activeElement.value = element
}

const addModule = () => {
  const newModule = {
    type: 'module',
    title: `Moduł ${course.value.modules.length + 1}`,
    lessons: []
  }
  course.value.modules.push(newModule)
  activeElement.value = newModule
}

const addLesson = (module) => {
  const newLesson = {
    type: 'lesson',
    title: `Nowa lekcja`,
    blocks: []
  }
  module.lessons.push(newLesson)
  activeElement.value = newLesson
}

const addBlock = (lesson, type) => {
  if (type === 'text') {
    lesson.blocks.push({ type: 'text', content: '' })
  } else if (type === 'video') {
    lesson.blocks.push({ type: 'video', url: '' })
  } else if (type === 'question') {
    lesson.blocks.push({ 
      type: 'question', 
      questionType: 'MultipleChoice', 
      content: '', 
      options: [
        { text: '', isCorrect: false },
        { text: '', isCorrect: false }
      ] 
    })
  }
}

const removeBlock = (lesson, index) => {
  lesson.blocks.splice(index, 1)
}

const addOption = (block) => {
  block.options.push({ text: '', isCorrect: false })
}

const removeOption = (block, index) => {
  block.options.splice(index, 1)
}

// pobierania danych do edycji kursu, jeśli jest to edycja istniejącego kursu
const fetchCourseForEditing = async (id) => {
  try {
    const token = localStorage.getItem('token')
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {}
    
    const response = await axios.get(`http://localhost:5042/api/courses/${id}/content`, config)
    const data = response.data
    
    isEditing.value = true
    course.value.id = data.id || id
    course.value.title = data.title
    course.value.description = data.description

    // odtoworzenie modułów i lekcji z danych backendu
    if (data.lessons && data.lessons.length > 0) {
      const loadedModule = {
        type: 'module',
        title: 'Lekcje kursu',
        lessons: data.lessons.map(lesson => ({
          type: 'lesson',
          title: lesson.title,
          blocks: lesson.blocks.map(block => {
            //mapowanie typów z backendu na typy w UI
            let uiType = 'text'
            if (block.type === 3) uiType = 'video'
            if (block.type === 1) uiType = 'question'

            return {
              type: uiType,
              content: block.textContent || block.questionText || '',
              url: block.videoUrl || '',
              questionType: 'MultipleChoice', 
              options: block.options && block.options.length > 0 ? block.options.map(opt => ({
                text: opt.optionText,
                isCorrect: opt.isCorrect
              })) : [ { text: '', isCorrect: false }, { text: '', isCorrect: false } ]
            }
          })
        }))
      }
      course.value.modules = [loadedModule]
    }
  } catch (error) {
    console.error("Błąd pobierania kursu:", error)
    errorMessage.value = "Nie udało się załadować kursu."
  }
}

// sprawdzenie czy mamy id
onMounted(() => {
  const courseId = route.params.id
  if (courseId) {
    fetchCourseForEditing(courseId)
  } else {
    // jeśli nie ma id, to jest nowy kurs, więc inicjalizujemy z jednym modułem
    addModule()
  }
})

// funkcja do zapisywania kursu (zarówno wersji roboczej jak i publikacji)
const saveCourseData = async (isPublished) => {
  isSaving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const flattenedLessons = course.value.modules.flatMap((module, mIdx) => 
      module.lessons.map((lesson, lIdx) => ({
        title: lesson.title,
        orderIndex: mIdx * 100 + lIdx + 1,
        blocks: lesson.blocks.map((block, bIdx) => {
          let blockType = 0
          if (block.type === 'video') blockType = 3
          if (block.type === 'question') blockType = 1

          return {
            type: blockType,
            orderIndex: bIdx + 1,
            textContent: block.type === 'text' ? block.content : null,
            videoUrl: block.type === 'video' ? block.url : null,
            questionText: block.type === 'question' ? block.content : null,
            options: block.type === 'question' ? block.options.map(opt => ({
              optionText: opt.text,
              isCorrect: opt.isCorrect
            })) : []
          }
        })
      }))
    )

    const payload = {
      id: course.value.id,
      isPublished: isPublished,
      title: course.value.title,
      description: course.value.description,
      lessons: flattenedLessons
    }

    const token = localStorage.getItem('token')
    const config = { headers: { Authorization: `Bearer ${token}` } }

    // Niezależnie czy to nowy kurs, czy edycja, wysyłamy POST na /save
    const response = await axios.post('http://localhost:5042/api/courses/save', payload, config)
    
    if (!course.value.id) {
      course.value.id = response.data.id
      isEditing.value = true 
      router.replace(`/course-creator/${course.value.id}`)
    }

    successMessage.value = isPublished ? "Kurs został opublikowany!" : "Zapisano wersję roboczą!"
    
    setTimeout(() => {
      successMessage.value = ''
    }, 3000)

  } catch (error) {
    console.error("Błąd podczas zapisywania kursu:", error)
    errorMessage.value = error.response?.data?.message || "Wystąpił błąd podczas zapisywania."
  } finally {
    isSaving.value = false
  }
}

const saveDraft = () => saveCourseData(false)
const publishCourse = () => saveCourseData(true)
</script>

<template>
  <div class="flex flex-col h-screen bg-floral-white text-charcoal-brown font-sans">
    
    <header class="sticky top-0 z-50 shrink-0 flex justify-between items-center px-6 py-4 bg-charcoal-brown text-floral-white shadow-md">
      <h1 class="text-xl font-bold tracking-wide">
        {{ isEditing ? 'Edytor Kursu' : 'Kreator Kursu' }}
      </h1>
      <div class="flex items-center gap-4">
        
        <span v-if="errorMessage" class="text-spicy-paprika text-sm font-medium">{{ errorMessage }}</span>
        <span v-else-if="isSaving" class="text-silver text-sm font-medium animate-pulse">Zapisywanie</span>
        <span v-else-if="successMessage" class="text-green-400 text-sm font-medium transition-opacity duration-500">{{ successMessage }}</span>
        
        <button 
          @click="saveDraft" 
          :disabled="isSaving"
          class="px-4 py-2 bg-silver/20 hover:bg-silver/40 disabled:opacity-50 text-floral-white font-medium rounded-lg transition-colors border border-silver ml-2"
        >
          Zapisz wersję roboczą
        </button>

        <button 
          @click="publishCourse" 
          :disabled="isSaving"
          class="px-5 py-2 bg-spicy-paprika hover:bg-spicy-paprika/90 disabled:bg-silver disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors shadow-sm"
        >
          {{ isEditing ? 'Zapisz zmiany (Publikuj)' : 'Opublikuj Kurs' }}
        </button>
      </div>
    </header>

    <div class="flex flex-1 overflow-hidden">
      
      <aside class="w-80 bg-charcoal-brown border-r border-silver/50 p-4 flex flex-col gap-4 overflow-y-auto shrink-0">
        <h3 class="text-lg font-semibold text-floral-white border-b border-silver/30 pb-2">Struktura kursu</h3>
        
        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-floral-white uppercase tracking-wider">Tytuł kursu</label>
          <input 
            v-model="course.title" 
            type="text" 
            class="bg-floral-white text-charcoal-brown w-full px-3 py-2 border border-silver rounded-md focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-sm"
          />
        </div>

        <div class="flex flex-col gap-1">
          <label class="text-xs font-bold text-floral-white uppercase tracking-wider">Opis</label>
          <textarea 
            v-model="course.description" 
            rows="2" 
            class="bg-floral-white text-charcoal-brown w-full px-3 py-2 border border-silver rounded-md focus:outline-none focus:ring-2 focus:ring-spicy-paprika text-sm resize-none"
          ></textarea>
        </div>

        <div class="my-2 border-t border-silver/30"></div>

        <div class="flex flex-col gap-2">
          <div v-for="(module, mIndex) in course.modules" :key="mIndex" class="flex flex-col gap-1">
            
            <div 
              @click="selectElement(module)"
              :class="[
                'px-3 py-2 border rounded-md shadow-sm cursor-pointer transition-all font-medium text-sm',
                activeElement === module ? 'bg-spicy-paprika border-spicy-paprika text-white ring-1 ring-spicy-paprika' : 'bg-carbon-black/20 border-silver/30 hover:border-spicy-paprika text-floral-white/90'
              ]"
            >
            {{ module.title }}
            </div>

            <div class="flex flex-col pl-4 gap-1 border-l-2 border-silver/30 ml-2">
              <div 
                v-for="(lesson, lIndex) in module.lessons" 
                :key="lIndex"
                @click="selectElement(lesson)"
                :class="[
                  'px-3 py-1.5 border rounded-md cursor-pointer transition-all text-sm',
                  activeElement === lesson ? 'bg-spicy-paprika border-spicy-paprika text-white' : 'bg-floral-white border-silver hover:border-spicy-paprika text-charcoal-brown'
                ]"
              >
            {{ lesson.title }}
              </div>
            </div>

          </div>
        </div>

        <button 
          @click="addModule"
          class="mt-2 px-4 py-2 w-full border-2 border-dashed border-silver/50 text-silver text-sm font-medium rounded-lg hover:border-spicy-paprika hover:text-floral-white transition-colors"
        >
          + Dodaj Moduł
        </button>
      </aside>

      <main class="flex-1 p-8 overflow-y-auto bg-floral-white">
        
        <div v-if="!activeElement" class="h-full flex items-center justify-center">
          <p class="text-silver text-lg">Wybierz element z menu po lewej, aby go edytować.</p>
        </div>
        
        <div v-else-if="activeElement.type === 'module'" class="w-full bg-white p-6 rounded-xl shadow-sm border border-silver">
          <h2 class="text-xl font-bold mb-6 text-charcoal-brown flex items-center gap-2">
          Edycja modułu
          </h2>

          <div class="flex flex-col gap-2 mb-6">
            <label class="text-sm font-semibold text-charcoal-brown">Tytuł modułu</label>
            <input 
              v-model="activeElement.title" 
              type="text" 
              class="w-full px-4 py-2 border border-silver bg-white text-charcoal-brown rounded-md focus:outline-none focus:ring-2 focus:ring-sky-blue"
            />
          </div>

          <div class="border-t border-silver pt-6">
            <button 
              @click="addLesson(activeElement)"
              class="px-5 py-2 bg-carbon-black hover:bg-charcoal-brown text-white text-sm font-medium rounded-lg transition-colors shadow-sm"
            >
              + Dodaj nową lekcję do tego modułu
            </button>
          </div>
        </div>

        <div v-else-if="activeElement.type === 'lesson'" class="w-full bg-white p-6 rounded-xl shadow-sm border border-silver">
          <h2 class="text-xl font-bold mb-6 text-charcoal-brown flex items-center gap-2">
          Edycja lekcji: {{ activeElement.title }}
          </h2>

          <div class="mb-6">
            <label class="text-sm font-semibold text-charcoal-brown">Tytuł lekcji</label>
            <input v-model="activeElement.title" type="text" class="w-full mt-1 px-4 py-2 border border-silver bg-white text-charcoal-brown rounded-md focus:outline-none focus:ring-2 focus:ring-sky-blue" />
          </div>

          <div class="border-t border-silver pt-6">
            <h3 class="text-sm font-bold text-silver uppercase tracking-wider mb-4">Zawartość lekcji</h3>

            <div class="flex flex-col gap-4 mb-6">
              <div v-for="(block, bIndex) in activeElement.blocks" :key="bIndex" class="relative group p-4 border border-silver rounded-lg bg-floral-white hover:border-silver/80 transition-colors">
                
                <button @click="removeBlock(activeElement, bIndex)" class="absolute top-2 right-2 text-silver hover:text-spicy-paprika opacity-0 group-hover:opacity-100 transition-opacity font-bold">✕</button>

                <div v-if="block.type === 'text'" class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-sky-blue uppercase">Blok Tekstowy (Markdown)</label>
                  <textarea v-model="block.content" rows="3" placeholder="Wpisz treść lekcji..." class="px-3 py-2 border border-silver bg-white text-charcoal-brown rounded-md focus:outline-none focus:ring-2 focus:ring-sky-blue resize-y"></textarea>
                </div>

                <div v-else-if="block.type === 'video'" class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-soft-coral uppercase">Blok Wideo</label>
                  <input v-model="block.url" type="text" placeholder="https://youtube.com/..." class="px-3 py-2 border border-silver bg-white text-charcoal-brown rounded-md focus:outline-none focus:ring-2 focus:ring-soft-coral" />
                </div>

                <div v-else-if="block.type === 'question'" class="flex flex-col gap-4">
                  <label class="text-xs font-bold text-amethyst uppercase">Blok Pytania Kontrolnego</label>
                  
                  <div class="flex gap-4">
                    <select v-model="block.questionType" class="px-3 py-2 border border-silver bg-white text-charcoal-brown rounded-md text-sm w-48 focus:outline-none focus:ring-2 focus:ring-amethyst">
                      <option value="MultipleChoice">Wielokrotny wybór</option>
                      <option value="TrueFalse">Prawda / Fałsz</option>
                      <option value="FillInTheBlank">Wpisz odpowiedź</option>
                    </select>
                    <input v-model="block.content" type="text" placeholder="Treść pytania..." class="flex-1 px-3 py-2 border border-silver bg-white text-charcoal-brown rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-amethyst" />
                  </div>

                  <div class="pl-4 border-l-2 border-amethyst/30">
                    <div v-for="(option, oIndex) in block.options" :key="oIndex" class="flex items-center gap-3 mb-2">
                      <input type="checkbox" v-model="option.isCorrect" class="w-4 h-4 text-amethyst border-silver rounded cursor-pointer focus:ring-amethyst" title="Zaznacz, jeśli poprawna" />
                      <input v-model="option.text" type="text" placeholder="Odpowiedź..." class="flex-1 px-3 py-1.5 border border-silver bg-white text-charcoal-brown rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-amethyst" />
                      <button @click="removeOption(block, oIndex)" class="text-xs text-silver hover:text-spicy-paprika font-bold">Usuń</button>
                    </div>
                    <button @click="addOption(block)" class="text-xs text-amethyst hover:text-amethyst/80 font-semibold mt-1">+ Dodaj odpowiedź</button>
                  </div>
                </div>

              </div>
            </div>

            <div class="flex gap-3 justify-center p-4 border-2 border-dashed border-silver rounded-lg bg-white">
              <span class="text-sm text-silver self-center mr-2">Dodaj nowy element:</span>
              <button @click="addBlock(activeElement, 'text')" class="px-4 py-2 bg-sky-blue/10 hover:bg-sky-blue/20 text-sky-blue text-sm font-semibold rounded-lg transition-colors">Tekst</button>
              <button @click="addBlock(activeElement, 'video')" class="px-4 py-2 bg-soft-coral/10 hover:bg-soft-coral/20 text-soft-coral text-sm font-semibold rounded-lg transition-colors">Wideo</button>
              <button @click="addBlock(activeElement, 'question')" class="px-4 py-2 bg-amethyst/10 hover:bg-amethyst/20 text-amethyst text-sm font-semibold rounded-lg transition-colors">Pytanie</button>
            </div>

          </div>
        </div>

      </main>
      
    </div>
  </div>
</template>