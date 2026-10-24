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
import SandboxView from '@/views/SandboxView.vue';
import SettingsView from '@/views/SettingsView.vue';
import AdminView from '@/views/AdminView.vue';

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
    meta: { requiresCourseRole: true },
  },
  {
    path: "/course-creator/:id",
    name: "course-editor",
    component: CourseCreatorView,
    meta: { requiresCourseRole: true },
  },
  {
    path: "/mentor-dashboard",
    name: "mentor-dashboard",
    component: MentorDashboardView,
    meta: { requiresCourseRole: true },
  },
  {
    path: "/dashboard",
    name: "dashboard",
    component: DashboardView,
  },
  {
    path: "/sandbox",
    name: "sandbox",
    component: SandboxView,
  },
  {
    path: "/settings",
    name: "settings",
    component: SettingsView,
  },
  {
    path: "/admin",
    name: "admin",
    component: AdminView,
    meta: { requiresAdmin: true },
  }
]


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

router.beforeEach((to) => {
  const token = localStorage.getItem('token')
  const role = localStorage.getItem('role')?.toLowerCase()

  if (!to.meta.requiresAdmin) {
    if (!to.meta.requiresCourseRole) {
      return true
    }

    if (!token) {
      return { name: 'login' }
    }

    if (role !== 'admin' && role !== 'mentor') {
      return { name: 'dashboard' }
    }

    return true
  }

  if (!token) {
    return { name: 'login' }
  }

  if (role !== 'admin') {
    return { name: 'dashboard' }
  }

  return true
})

export default router
