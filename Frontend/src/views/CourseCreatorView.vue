<script lang="ts" setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'

// --- INTERFEJSY ---
interface QuestionOption {
  text: string
  isCorrect: boolean
}

type BlockType = 'text' | 'video' | 'question'
type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'TextAnswer'

interface CourseBlock {
  type: BlockType
  content?: string
  url?: string
  questionType?: QuestionType
  hint?: string
  options?: QuestionOption[]
}

interface CourseLesson {
  type: 'lesson'
  title: string
  blocks: CourseBlock[]
}

interface CourseModule {
  type: 'module'
  title: string
  orderIndex: number
  lessons: CourseLesson[]
}

interface Course {
  id: string | number | null
  title: string
  description: string
  modules: CourseModule[]
}

// Interfejsy dla danych przychodzących z API (DTO)
interface ApiOptionDto {
  optionText: string
  isCorrect: boolean
}

interface ApiBlockDto {
  type: number
  questionType?: QuestionType
  textContent?: string
  questionText?: string
  videoUrl?: string
  hint?: string
  options?: ApiOptionDto[]
}

interface ApiLessonDto {
  title: string
  orderIndex: number
  blocks: ApiBlockDto[]
}

interface ApiModuleDto {
  title: string
  order: number
  lessons: ApiLessonDto[]
}

// --- LOGIKA KOMPONENTU ---
const route = useRoute()
const router = useRouter()

const course = ref<Course>({
  id: null,
  title: '',
  description: '',
  modules: []
})

const isEditing = ref(false)
const activeElement = ref<CourseModule | CourseLesson | null>(null)
const isSaving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const getApiErrorMessage = (error: unknown, fallback: string): string => {
  if (axios.isAxiosError(error)) {
    return error.response?.data?.message || fallback
  }

  if (error instanceof Error) {
    return error.message || fallback
  }

  return fallback
}

const selectElement = (element: CourseModule | CourseLesson) => {
  activeElement.value = element
}

const addModule = () => {
  const newModule: CourseModule = {
    type: 'module',
    title: `Moduł ${course.value.modules.length + 1}`,
    orderIndex: course.value.modules.length + 1,
    lessons: []
  }
  course.value.modules.push(newModule)
  activeElement.value = newModule
}

const addLesson = (module: CourseModule) => {
  const newLesson: CourseLesson = {
    type: 'lesson',
    title: `Nowa lekcja`,
    blocks: []
  }
  module.lessons.push(newLesson)
  activeElement.value = newLesson
}

const addBlock = (lesson: CourseLesson, type: BlockType) => {
  if (type === 'text') {
    lesson.blocks.push({ type: 'text', content: '' })
  } else if (type === 'video') {
    lesson.blocks.push({ type: 'video', url: '' })
  } else if (type === 'question') {
    lesson.blocks.push({ 
      type: 'question', 
      questionType: 'MultipleChoice', 
      content: '', 
      hint: '',
      options: [
        { text: '', isCorrect: false },
        { text: '', isCorrect: false }
      ] 
    })
  }
}

const removeBlock = (lesson: CourseLesson, index: number) => {
  lesson.blocks.splice(index, 1)
}

const addOption = (block: CourseBlock) => {
  if (!block.options) {
    block.options = []
  }
  block.options.push({ text: '', isCorrect: false })
}

const removeOption = (block: CourseBlock, index: number) => {
  block.options?.splice(index, 1)
}

const onOptionCorrectChange = (block: CourseBlock, selectedIndex: number) => {
  if (block.questionType !== 'TrueFalse' || !block.options || !block.options[selectedIndex]?.isCorrect) {
    return
  }

  block.options.forEach((option, index) => {
    if (index !== selectedIndex) {
      option.isCorrect = false
    }
  })
}

const onQuestionTypeChange = (block: CourseBlock) => {
  if (block.questionType === 'TrueFalse' && (!block.options || block.options.every(option => !option.text.trim()))) {
    block.options = [
      { text: 'Prawda', isCorrect: false },
      { text: 'Fałsz', isCorrect: false }
    ]
  }
}

const fetchCourseForEditing = async (id: string) => {
  try {
    const token = localStorage.getItem('token')
    const config = token ? { headers: { Authorization: `Bearer ${token}` } } : {}
    
    const response = await axios.get(`/api/courses/${id}/content`, config)
    const data = response.data
    
    isEditing.value = true
    course.value.id = data.id || id
    course.value.title = data.title
    course.value.description = data.description

    const mapLesson = (lesson: ApiLessonDto): CourseLesson => ({
      type: 'lesson',
      title: lesson.title,
      blocks: (lesson.blocks || []).map((block: ApiBlockDto): CourseBlock => {
        let uiType: BlockType = 'text'
        if (block.type === 3) uiType = 'video'
        if (block.type === 1) uiType = 'question'

        return {
          type: uiType,
          content: block.textContent || block.questionText || '',
          url: block.videoUrl || '',
          hint: block.hint || '',
          questionType: block.questionType ?? 'MultipleChoice',
          options: block.options && block.options.length > 0 ? block.options.map((opt: ApiOptionDto) => ({
            text: opt.optionText,
            isCorrect: opt.isCorrect
          })) : [ { text: '', isCorrect: false }, { text: '', isCorrect: false } ]
        }
      })
    })

    if (data.modules && data.modules.length > 0) {
      course.value.modules = [...data.modules]
        .sort((a: ApiModuleDto, b: ApiModuleDto) => a.order - b.order)
        .map((module: ApiModuleDto): CourseModule => ({
          type: 'module',
          title: module.title,
          orderIndex: module.order,
          lessons: [...(module.lessons || [])]
            .sort((a: ApiLessonDto, b: ApiLessonDto) => a.orderIndex - b.orderIndex)
            .map(mapLesson)
        }))
    } else if (data.lessons && data.lessons.length > 0) {
      const loadedModule: CourseModule = {
        type: 'module',
        title: 'Lekcje kursu',
        orderIndex: 1,
        lessons: data.lessons.map(mapLesson)
      }
      course.value.modules = [loadedModule]
    }
  } catch (error: unknown) {
    console.error("Błąd pobierania kursu:", error)
    errorMessage.value = getApiErrorMessage(error, "Nie udało się załadować kursu.")
  }
}

onMounted(() => {
  const rawId = route.params.id
  const courseId = Array.isArray(rawId) ? rawId[0] : rawId
  if (courseId) {
    fetchCourseForEditing(courseId)
  } else {
    addModule()
  }
})

const saveCourseData = async (isPublished: boolean) => {
  isSaving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const modules = course.value.modules.map((module: CourseModule, mIdx: number) => ({
      title: module.title,
      orderIndex: mIdx + 1,
      lessons: module.lessons.map((lesson: CourseLesson, lIdx: number) => ({
        title: lesson.title,
        orderIndex: mIdx * 100 + lIdx + 1,
        blocks: lesson.blocks.map((block: CourseBlock, bIdx: number) => {
          let blockType = 0
          if (block.type === 'video') blockType = 3
          if (block.type === 'question') blockType = 1

          return {
            type: blockType,
            orderIndex: bIdx + 1,
            textContent: block.type === 'text' ? (block.content ?? null) : null,
            videoUrl: block.type === 'video' ? (block.url ?? null) : null,
            questionText: block.type === 'question' ? (block.content ?? null) : null,
            questionType: block.type === 'question' ? (block.questionType ?? 'MultipleChoice') : 'MultipleChoice',
            hint: block.type === 'question' ? (block.hint ?? null) : null,
            options: block.type === 'question' && block.options ? block.options.map((opt: QuestionOption) => ({
              optionText: opt.text,
              isCorrect: block.questionType === 'TextAnswer' ? !!opt.text.trim() : opt.isCorrect
            })) : []
          }
        })
      }))
    }))

    const flattenedLessons = modules.flatMap(module => module.lessons)

    const payload = {
      id: course.value.id,
      isPublished: isPublished,
      title: course.value.title,
      description: course.value.description,
      modules: modules,
      lessons: flattenedLessons
    }

    const token = localStorage.getItem('token')
    const config = { headers: { Authorization: `Bearer ${token}` } }

    const response = await axios.post('/api/courses/save', payload, config)
    
    if (!course.value.id) {
      course.value.id = response.data.id
      isEditing.value = true 
      router.replace(`/course-creator/${course.value.id}`)
    }

    successMessage.value = isPublished ? "Kurs został opublikowany!" : "Zapisano wersję roboczą!"
    
    setTimeout(() => {
      successMessage.value = ''
    }, 3000)

  } catch (error: unknown) {
    console.error("Błąd podczas zapisywania kursu:", error)
    errorMessage.value = getApiErrorMessage(error, "Wystąpił błąd podczas zapisywania.")
  } finally {
    isSaving.value = false
  }
}

const saveDraft = () => saveCourseData(false)
const publishCourse = () => saveCourseData(true)
</script>

<template>
  <div class="flex flex-col h-screen bg-floral-white text-charcoal-brown font-sans">
    <header class="sticky top-0 z-10 flex justify-between items-center px-6 py-4 bg-charcoal-brown text-floral-white shadow-md">
      <h1 class="text-xl font-bold">{{ isEditing ? 'Edytor kursu' : 'Kreator kursu' }}</h1>
      <div class="flex items-center gap-4">
        <span v-if="errorMessage" class="text-spicy-paprika text-sm">{{ errorMessage }}</span>
        <span v-else-if="isSaving" class="text-silver text-sm animate-pulse">Zapisywanie</span>
        <span v-else-if="successMessage" class="text-green-400 text-sm">{{ successMessage }}</span>
        <button @click="saveDraft" :disabled="isSaving" class="px-4 py-2 border border-silver text-floral-white rounded-lg disabled:opacity-50">Zapisz wersję roboczą</button>
        <button @click="publishCourse" :disabled="isSaving" class="px-5 py-2 bg-spicy-paprika text-white rounded-lg disabled:opacity-50">{{ isEditing ? 'Zapisz zmiany' : 'Opublikuj kurs' }}</button>
      </div>
    </header>

    <div class="flex flex-1 overflow-hidden">
      <aside class="w-80 bg-charcoal-brown border-r border-silver/50 p-4 flex flex-col gap-4 overflow-y-auto shrink-0">
        <h2 class="text-lg font-semibold text-floral-white border-b border-silver/30 pb-2">Struktura kursu</h2>
        <label class="text-xs font-bold text-floral-white">Tytuł kursu
          <input v-model="course.title" type="text" class="mt-1 bg-floral-white text-charcoal-brown w-full px-3 py-2 border border-silver rounded-md" />
        </label>
        <label class="text-xs font-bold text-floral-white">Opis
          <textarea v-model="course.description" rows="2" class="mt-1 bg-floral-white text-charcoal-brown w-full px-3 py-2 border border-silver rounded-md resize-y"></textarea>
        </label>
        <div class="border-t border-silver/30"></div>
        <div v-for="(module, mIndex) in course.modules" :key="mIndex" class="flex flex-col gap-1">
          <button @click="selectElement(module)" :class="['px-3 py-2 border rounded-md text-left text-sm', activeElement === module ? 'bg-spicy-paprika text-white' : 'text-floral-white border-silver/30']">{{ module.title }}</button>
          <div class="flex flex-col pl-4 gap-1 border-l-2 border-silver/30 ml-2">
            <button v-for="(lesson, lIndex) in module.lessons" :key="lIndex" @click="selectElement(lesson)" :class="['px-3 py-1.5 border rounded-md text-left text-sm', activeElement === lesson ? 'bg-spicy-paprika text-white' : 'bg-floral-white text-charcoal-brown border-silver']">{{ lesson.title }}</button>
          </div>
        </div>
        <button @click="addModule" class="mt-2 px-4 py-2 w-full border-2 border-dashed border-silver/50 text-silver text-sm rounded-lg">+ Dodaj moduł</button>
      </aside>

      <main class="flex-1 p-8 overflow-y-auto bg-floral-white">
        <div v-if="!activeElement" class="h-full flex items-center justify-center text-silver">Wybierz element do edycji.</div>
        <section v-else-if="activeElement.type === 'module'" class="bg-white p-6 rounded-lg border border-silver">
          <h2 class="text-xl font-bold mb-6">Edycja modułu</h2>
          <label class="text-sm font-semibold">Tytuł modułu
            <input v-model="activeElement.title" type="text" class="w-full mt-1 px-4 py-2 border border-silver rounded-md" />
          </label>
          <div class="border-t border-silver mt-6 pt-6">
            <button @click="addLesson(activeElement)" class="px-5 py-2 bg-carbon-black text-white rounded-md">+ Dodaj lekcję do modułu</button>
          </div>
        </section>

        <section v-else-if="activeElement.type === 'lesson'" class="bg-white p-6 rounded-lg border border-silver">
          <h2 class="text-xl font-bold mb-6">Edycja lekcji</h2>
          <label class="text-sm font-semibold">Tytuł lekcji
            <input v-model="activeElement.title" type="text" class="w-full mt-1 px-4 py-2 border border-silver rounded-md" />
          </label>
          <div class="border-t border-silver mt-6 pt-6">
            <h3 class="text-sm font-bold text-silver uppercase mb-4">Zawartość lekcji</h3>
            <div class="flex flex-col gap-4 mb-6">
              <article v-for="(block, bIndex) in activeElement.blocks" :key="bIndex" class="relative p-4 border border-silver rounded-lg bg-floral-white">
                <button @click="removeBlock(activeElement, bIndex)" class="absolute top-2 right-2 text-silver hover:text-spicy-paprika" aria-label="Usuń blok">✕</button>
                <div v-if="block.type === 'text'" class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-sky-blue">Blok tekstowy</label>
                  <textarea v-model="block.content" rows="3" placeholder="Wpisz treść lekcji..." class="px-3 py-2 border border-silver rounded-md"></textarea>
                </div>
                <div v-else-if="block.type === 'video'" class="flex flex-col gap-2">
                  <label class="text-xs font-bold text-soft-coral">Blok wideo</label>
                  <input v-model="block.url" type="url" placeholder="https://youtube.com/..." class="px-3 py-2 border border-silver rounded-md" />
                </div>
                <div v-else class="flex flex-col gap-4">
                  <label class="text-xs font-bold text-amethyst">Typ pytania</label>
                  <div class="flex flex-col sm:flex-row gap-3">
                    <select v-model="block.questionType" @change="onQuestionTypeChange(block)" class="px-3 py-2 border border-silver bg-white rounded-md text-sm">
                      <option value="MultipleChoice">Wielokrotny wybór</option>
                      <option value="TrueFalse">Prawda / Fałsz</option>
                      <option value="TextAnswer">Odpowiedź tekstowa</option>
                    </select>
                    <input v-model="block.content" type="text" placeholder="Treść pytania..." class="flex-1 px-3 py-2 border border-silver rounded-md text-sm" />
                  </div>
                  <input v-model="block.hint" type="text" placeholder="Podpowiedź dla ucznia (opcjonalnie)" class="w-full px-3 py-2 border border-silver rounded-md text-sm" />
                  <div class="pl-4 border-l-2 border-amethyst/30">
                    <p class="text-xs text-silver mb-2">{{ block.questionType === 'TextAnswer' ? 'Akceptowane odpowiedzi' : block.questionType === 'TrueFalse' ? 'Opcje prawda/fałsz, zaznacz poprawną' : 'Opcje odpowiedzi, zaznacz wszystkie poprawne' }}</p>
                    <div v-for="(option, oIndex) in block.options" :key="oIndex" class="flex items-center gap-3 mb-2">
                      <input v-if="block.questionType !== 'TextAnswer'" v-model="option.isCorrect" @change="onOptionCorrectChange(block, oIndex)" type="checkbox" class="w-4 h-4" :aria-label="`Poprawna odpowiedź ${oIndex + 1}`" />
                      <input v-model="option.text" type="text" :placeholder="block.questionType === 'TextAnswer' ? 'Akceptowana odpowiedź...' : 'Odpowiedź...'" class="flex-1 px-3 py-1.5 border border-silver rounded-md text-sm" />
                      <button @click="removeOption(block, oIndex)" class="text-xs text-silver hover:text-spicy-paprika">Usuń</button>
                    </div>
                    <button v-if="block.questionType !== 'TrueFalse'" @click="addOption(block)" class="text-xs text-amethyst font-semibold mt-1">+ Dodaj odpowiedź</button>
                  </div>
                </div>
              </article>
            </div>
            <div class="flex flex-wrap gap-3 justify-center p-4 border-2 border-dashed border-silver rounded-lg">
              <span class="text-sm text-silver self-center">Dodaj element:</span>
              <button @click="addBlock(activeElement, 'text')" class="px-4 py-2 bg-sky-blue/10 text-sky-blue text-sm font-semibold rounded-md">Tekst</button>
              <button @click="addBlock(activeElement, 'video')" class="px-4 py-2 bg-soft-coral/10 text-soft-coral text-sm font-semibold rounded-md">Wideo</button>
              <button @click="addBlock(activeElement, 'question')" class="px-4 py-2 bg-amethyst/10 text-amethyst text-sm font-semibold rounded-md">Pytanie</button>
            </div>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>