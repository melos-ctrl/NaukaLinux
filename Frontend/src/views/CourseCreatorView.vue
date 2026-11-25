<script lang="ts" setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import axios from 'axios'
import MarkdownEditor from '@/components/MarkdownEditor.vue'

// --- INTERFEJSY ---
// Pola `id` to ID z bazy. Przy edycji odsyłamy je do backendu, żeby zaktualizował istniejące
// elementy zamiast kasować i tworzyć je od nowa (co usuwało odpowiedzi kursantów).
interface QuestionOption {
  id?: number
  text: string
  isCorrect: boolean
}

type BlockType = 'text' | 'video' | 'question'
type QuestionType = 'MultipleChoice' | 'TrueFalse' | 'TextAnswer'

interface CourseBlock {
  id?: number
  type: BlockType
  content?: string
  url?: string
  questionType?: QuestionType
  hint?: string
  options?: QuestionOption[]
  // Pola bez edytora w kreatorze - przechowujemy je, żeby zapis ich nie skasował
  frontText?: string | null
  backText?: string | null
}

interface CourseLesson {
  id?: number
  type: 'lesson'
  title: string
  blocks: CourseBlock[]
}

interface CourseModule {
  id?: number
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
  id: number
  optionText: string
  isCorrect: boolean
}

interface ApiBlockDto {
  id: number
  type: number
  orderIndex: number
  questionType?: QuestionType
  textContent?: string
  questionText?: string
  videoUrl?: string
  hint?: string
  frontText?: string | null
  backText?: string | null
  options?: ApiOptionDto[]
}

interface ApiLessonDto {
  id: number
  title: string
  orderIndex: number
  blocks: ApiBlockDto[]
}

interface ApiModuleDto {
  id: number
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
const isPublished = ref(false)
const activeElement = ref<CourseModule | CourseLesson | null>(null)
const isSaving = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

const getAuthConfig = () => {
  const token = localStorage.getItem('token')
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {}
}

const getApiErrorMessage = (error: unknown, fallback: string): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data
    if (typeof data === 'string' && data) return data
    return data?.message || fallback
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

// Zapamiętuje, który element był zaznaczony (po pozycji), żeby po ponownym wczytaniu kursu go przywrócić
const getActivePath = (): { moduleIndex: number; lessonIndex: number | null } | null => {
  const active = activeElement.value
  if (!active) return null

  for (let moduleIndex = 0; moduleIndex < course.value.modules.length; moduleIndex++) {
    const module = course.value.modules[moduleIndex]
    if (!module) continue
    if (module === active) return { moduleIndex, lessonIndex: null }
    const lessonIndex = module.lessons.indexOf(active as CourseLesson)
    if (lessonIndex !== -1) return { moduleIndex, lessonIndex }
  }
  return null
}

const restoreActivePath = (path: { moduleIndex: number; lessonIndex: number | null } | null) => {
  if (!path) return
  const module = course.value.modules[path.moduleIndex]
  if (!module) return
  activeElement.value = path.lessonIndex === null ? module : (module.lessons[path.lessonIndex] ?? module)
}

const fetchCourseForEditing = async (id: string | number) => {
  try {
    const response = await axios.get(`/api/courses/${id}/content`, getAuthConfig())
    const data = response.data

    isEditing.value = true
    isPublished.value = !!data.isPublished
    course.value.id = data.id || id
    course.value.title = data.title
    course.value.description = data.description

    const mapLesson = (lesson: ApiLessonDto): CourseLesson => ({
      id: lesson.id,
      type: 'lesson',
      title: lesson.title,
      blocks: [...(lesson.blocks || [])]
        .sort((a, b) => a.orderIndex - b.orderIndex)
        .map((block: ApiBlockDto): CourseBlock => {
          let uiType: BlockType = 'text'
          if (block.type === 3) uiType = 'video'
          if (block.type === 1) uiType = 'question'

          return {
            id: block.id,
            type: uiType,
            content: block.textContent || block.questionText || '',
            url: block.videoUrl || '',
            hint: block.hint || '',
            questionType: block.questionType ?? 'MultipleChoice',
            frontText: block.frontText ?? null,
            backText: block.backText ?? null,
            options: block.options && block.options.length > 0
              ? block.options.map((opt: ApiOptionDto) => ({
                  id: opt.id,
                  text: opt.optionText,
                  isCorrect: opt.isCorrect
                }))
              : [ { text: '', isCorrect: false }, { text: '', isCorrect: false } ]
          }
        })
    })

    if (data.modules && data.modules.length > 0) {
      course.value.modules = [...data.modules]
        .sort((a: ApiModuleDto, b: ApiModuleDto) => a.order - b.order)
        .map((module: ApiModuleDto): CourseModule => ({
          id: module.id,
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

const buildPayload = (publish: boolean) => {
  const modules = course.value.modules.map((module: CourseModule, mIdx: number) => ({
    id: module.id ?? null,
    title: module.title,
    orderIndex: mIdx + 1,
    lessons: module.lessons.map((lesson: CourseLesson, lIdx: number) => ({
      id: lesson.id ?? null,
      title: lesson.title,
      orderIndex: mIdx * 100 + lIdx + 1,
      blocks: lesson.blocks.map((block: CourseBlock, bIdx: number) => {
        let blockType = 0
        if (block.type === 'video') blockType = 3
        if (block.type === 'question') blockType = 1

        return {
          id: block.id ?? null,
          type: blockType,
          orderIndex: bIdx + 1,
          textContent: block.type === 'text' ? (block.content ?? null) : null,
          videoUrl: block.type === 'video' ? (block.url ?? null) : null,
          questionText: block.type === 'question' ? (block.content ?? null) : null,
          questionType: block.type === 'question' ? (block.questionType ?? 'MultipleChoice') : 'MultipleChoice',
          hint: block.type === 'question' ? (block.hint ?? null) : null,
          frontText: block.frontText ?? null,
          backText: block.backText ?? null,
          options: block.type === 'question' && block.options ? block.options.map((opt: QuestionOption) => ({
            id: opt.id ?? null,
            optionText: opt.text,
            isCorrect: block.questionType === 'TextAnswer' ? !!opt.text.trim() : opt.isCorrect
          })) : []
        }
      })
    }))
  }))

  return {
    id: course.value.id,
    isPublished: publish,
    title: course.value.title,
    description: course.value.description,
    modules: modules
  }
}

const saveCourseData = async (publish: boolean) => {
  isSaving.value = true
  errorMessage.value = ''
  successMessage.value = ''

  try {
    const response = await axios.post('/api/courses/save', buildPayload(publish), getAuthConfig())
    const savedId = response.data.id

    isPublished.value = !!response.data.isPublished

    if (!course.value.id) {
      course.value.id = savedId
      isEditing.value = true
      router.replace(`/course-creator/${savedId}`)
    }

    // Wczytujemy kurs ponownie, żeby nowe moduły/lekcje/bloki dostały ID z bazy.
    // Bez tego kolejny zapis potraktowałby je jako nowe i utworzył jeszcze raz.
    const activePath = getActivePath()
    await fetchCourseForEditing(savedId)
    restoreActivePath(activePath)

    successMessage.value = response.data.message
      || (publish ? "Kurs został opublikowany!" : "Zapisano wersję roboczą!")

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
const saveChanges = () => saveCourseData(isPublished.value)

const unpublishCourse = () => {
  if (!confirm('Cofnąć publikację? Kurs zniknie z katalogu, ale postępy kursantów zostaną zachowane.')) return
  saveCourseData(false)
}
</script>

<template>
  <div class="fullscreen-view flex flex-col bg-floral-white text-carbon-black">
    <!-- Pasek narzędzi kreatora (jasny - ciemny jest już header aplikacji) -->
    <div class="flex shrink-0 flex-wrap items-center justify-between gap-4 border-b border-line bg-white px-6 py-4">
      <div class="flex items-center gap-3">
        <h1 class="section-title">{{ isEditing ? 'Edytor kursu' : 'Kreator kursu' }}</h1>
        <span
          v-if="isEditing"
          class="badge"
          :class="isPublished ? 'badge-success' : 'badge-warning'"
        >
          {{ isPublished ? 'Opublikowany' : 'Szkic' }}
        </span>
      </div>
      <div class="flex flex-wrap items-center gap-3">
        <span v-if="errorMessage" class="max-w-md text-right text-sm text-red-700" role="alert">{{ errorMessage }}</span>
        <span v-else-if="isSaving" class="text-sm text-muted animate-pulse" role="status">Zapisywanie...</span>
        <span v-else-if="successMessage" class="text-sm font-medium text-green-700" role="status">{{ successMessage }}</span>

        <!-- Szkic: zapisz szkic albo opublikuj -->
        <template v-if="!isPublished">
          <button type="button" @click="saveDraft" :disabled="isSaving" class="btn btn-outline">Zapisz szkic</button>
          <button type="button" @click="publishCourse" :disabled="isSaving" class="btn btn-primary">Opublikuj kurs</button>
        </template>
        <!-- Opublikowany: zapis zmian nie zmienia statusu; cofnięcie publikacji jest osobną, świadomą akcją -->
        <template v-else>
          <button type="button" @click="unpublishCourse" :disabled="isSaving" class="btn btn-outline">Cofnij publikację</button>
          <button type="button" @click="saveChanges" :disabled="isSaving" class="btn btn-primary">Zapisz zmiany</button>
        </template>
      </div>
    </div>

    <div class="flex min-h-0 flex-1 gap-6 overflow-hidden p-4 md:p-6">
      <aside class="flex w-80 shrink-0 flex-col gap-4 overflow-y-auto rounded-2xl bg-charcoal-brown p-4 text-floral-white shadow-lg">
        <h2 class="border-b border-silver/20 pb-2 font-bold">Struktura kursu</h2>
        <div>
          <label for="course-title" class="mb-1.5 block text-sm font-semibold">Tytuł kursu</label>
          <input id="course-title" v-model="course.title" type="text" class="input" />
        </div>
        <div>
          <label for="course-description" class="mb-1.5 block text-sm font-semibold">Opis</label>
          <textarea id="course-description" v-model="course.description" rows="2" class="input resize-y"></textarea>
        </div>
        <div class="border-t border-silver/20"></div>
        <div v-for="(module, mIndex) in course.modules" :key="mIndex" class="flex flex-col gap-1">
          <button
            type="button"
            @click="selectElement(module)"
            class="side-link font-semibold"
            :class="{ 'side-link-active': activeElement === module }"
          >{{ module.title }}</button>
          <div class="ml-2 flex flex-col gap-1 border-l-2 border-silver/20 pl-3">
            <button
              v-for="(lesson, lIndex) in module.lessons"
              :key="lIndex"
              type="button"
              @click="selectElement(lesson)"
              class="side-link py-2 text-sm"
              :class="{ 'side-link-active': activeElement === lesson }"
            >{{ lesson.title }}</button>
          </div>
        </div>
        <button type="button" @click="addModule" class="btn btn-outline-dark mt-2 w-full border-dashed">+ Dodaj moduł</button>
      </aside>

      <main class="min-w-0 flex-1 overflow-y-auto">
        <p v-if="!activeElement" class="empty-state">Wybierz element do edycji.</p>

        <section v-else-if="activeElement.type === 'module'" class="card space-y-6">
          <h2 class="section-title">Edycja modułu</h2>
          <div>
            <label for="module-title" class="label">Tytuł modułu</label>
            <input id="module-title" v-model="activeElement.title" type="text" class="input" />
          </div>
          <div class="border-t border-line pt-6">
            <button type="button" @click="addLesson(activeElement)" class="btn btn-dark">+ Dodaj lekcję do modułu</button>
          </div>
        </section>

        <section v-else-if="activeElement.type === 'lesson'" class="card space-y-6">
          <h2 class="section-title">Edycja lekcji</h2>
          <div>
            <label for="lesson-title" class="label">Tytuł lekcji</label>
            <input id="lesson-title" v-model="activeElement.title" type="text" class="input" />
          </div>
          <div class="border-t border-line pt-6">
            <h3 class="mb-4 text-sm font-semibold uppercase tracking-wider text-muted">Zawartość lekcji</h3>
            <div class="mb-6 flex flex-col gap-4">
              <article v-for="(block, bIndex) in activeElement.blocks" :key="bIndex" class="relative rounded-xl border border-line bg-floral-white p-4">
                <button type="button" @click="removeBlock(activeElement, bIndex)" class="absolute right-2 top-2 flex h-8 w-8 items-center justify-center rounded-lg text-muted hover:bg-red-50 hover:text-red-700" aria-label="Usuń blok">✕</button>

                <div v-if="block.type === 'text'" class="flex flex-col gap-2">
                  <span class="badge badge-info">Blok tekstowy</span>
                  <MarkdownEditor v-model="block.content" placeholder="Wpisz treść lekcji..." aria-label="Treść bloku tekstowego" />
                </div>

                <div v-else-if="block.type === 'video'" class="flex flex-col gap-2">
                  <span class="badge badge-primary">Blok wideo</span>
                  <input v-model="block.url" type="url" placeholder="https://youtube.com/..." aria-label="Adres filmu" class="input" />
                </div>

                <div v-else class="flex flex-col gap-4">
                  <span class="badge bg-amethyst/10 text-[#6d3a96]">Pytanie</span>
                  <div class="flex flex-col gap-3 sm:flex-row">
                    <select v-model="block.questionType" @change="onQuestionTypeChange(block)" aria-label="Typ pytania" class="input sm:w-56">
                      <option value="MultipleChoice">Wielokrotny wybór</option>
                      <option value="TrueFalse">Prawda / Fałsz</option>
                      <option value="TextAnswer">Odpowiedź tekstowa</option>
                    </select>
                    <input v-model="block.content" type="text" placeholder="Treść pytania..." aria-label="Treść pytania" class="input flex-1" />
                  </div>
                  <input v-model="block.hint" type="text" placeholder="Podpowiedź dla ucznia (opcjonalnie)" aria-label="Podpowiedź" class="input" />
                  <div class="border-l-2 border-amethyst/30 pl-4">
                    <p class="mb-2 text-xs text-muted">{{ block.questionType === 'TextAnswer' ? 'Akceptowane odpowiedzi' : block.questionType === 'TrueFalse' ? 'Opcje prawda/fałsz, zaznacz poprawną' : 'Opcje odpowiedzi, zaznacz wszystkie poprawne' }}</p>
                    <div v-for="(option, oIndex) in block.options" :key="oIndex" class="mb-2 flex items-center gap-3">
                      <input v-if="block.questionType !== 'TextAnswer'" v-model="option.isCorrect" @change="onOptionCorrectChange(block, oIndex)" type="checkbox" class="h-4 w-4 accent-paprika-600" :aria-label="`Poprawna odpowiedź ${oIndex + 1}`" />
                      <input v-model="option.text" type="text" :placeholder="block.questionType === 'TextAnswer' ? 'Akceptowana odpowiedź...' : 'Odpowiedź...'" :aria-label="`Odpowiedź ${oIndex + 1}`" class="input h-10 flex-1 text-sm" />
                      <button type="button" @click="removeOption(block, oIndex)" class="btn btn-ghost btn-sm text-muted hover:text-red-700">Usuń</button>
                    </div>
                    <button v-if="block.questionType !== 'TrueFalse'" type="button" @click="addOption(block)" class="btn btn-ghost btn-sm">+ Dodaj odpowiedź</button>
                  </div>
                </div>
              </article>
            </div>
            <div class="flex flex-wrap justify-center gap-3 rounded-xl border-2 border-dashed border-line-strong p-4">
              <span class="self-center text-sm text-muted">Dodaj element:</span>
              <button type="button" @click="addBlock(activeElement, 'text')" class="btn btn-outline btn-sm">Tekst</button>
              <button type="button" @click="addBlock(activeElement, 'video')" class="btn btn-outline btn-sm">Wideo</button>
              <button type="button" @click="addBlock(activeElement, 'question')" class="btn btn-outline btn-sm">Pytanie</button>
            </div>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>
