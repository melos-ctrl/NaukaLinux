import { createRouter, createWebHistory, type RouteRecordRaw} from 'vue-router';
import LessonView from '../views/LessonView.vue';
import LoginView from '@/views/LoginView.vue';
import RegisterView from '@/views/RegisterView.vue';
import CoursesView from '@/views/CoursesView.vue';
import HomeView from '@/views/HomeView.vue';
import CourseCreatorView from '@/views/CourseCreatorView.vue';

const routes: Array<RouteRecordRaw> = [
  {
    path: "/lesson",
    name: "lesson",
    component: LessonView,
  },
  {
    path: "/login",
    name: "login",
    component: LoginView,
  },
  {
    path: "/register",
    name: "register",
    component: RegisterView,
  },
  {
    path: "/courses",
    name: "courses",
    component: CoursesView,
  },
  {
    path: "/",
    name: "home",
    component: HomeView,
  },
  {
    path: "/course-creator",
    name: "course-creator",
    component: CourseCreatorView,
  }

]


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

export default router
