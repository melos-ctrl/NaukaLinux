import { createRouter, createWebHistory, type RouteRecordRaw} from 'vue-router';
import LessonView from '../views/LessonView.vue';
import LoginView from '@/views/LoginView.vue';
import RegisterView from '@/views/RegisterView.vue';
import CoursesView from '@/views/CoursesView.vue';
import HomeView from '@/views/HomeView.vue';
import CourseCreatorView from '@/views/CourseCreatorView.vue';
import MentorDashboardView from '@/views/MentorDashboardView.vue';
import DashboardView from '@/views/DashboardView.vue';
import ForgotPasswordView from '@/views/ForgotPasswordView.vue';
import ResetPasswordView from '@/views/ResetPasswordView.vue';

const routes: Array<RouteRecordRaw> = [
  {
    path: "/course/:id",
    name: "course-player",
    component: LessonView,
  },
  {
    path: "/forgot-password",
    name: "forgot-password",
    component: ForgotPasswordView,
  },
  {
    path: "/reset-password",
    name: "reset-password",
    component: ResetPasswordView,
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
  },
  {
    path: "/course-creator/:id",
    name: "course-editor",
    component: CourseCreatorView,
  },
  {
    path: "/mentor-dashboard",
    name: "mentor-dashboard",
    component: MentorDashboardView,
  },
  {
    path: "/dashboard",
    name: "dashboard",
    component: DashboardView,
  }

]


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

export default router
