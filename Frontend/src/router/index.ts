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
import BeAMentorView from '@/views/BeAMentorView.vue';
import VerifyEmailView from '@/views/VerifyEmailView.vue';
import { getHomeRouteForRole } from '@/utils/roles';

const routes: Array<RouteRecordRaw> = [
  {
    path: "/course/:id",
    name: "course-player",
    component: LessonView,
    meta: { requiresAuth: true },
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
    meta: { guestOnly: true },
  },
  {
    path: "/register",
    name: "register",
    component: RegisterView,
    meta: { guestOnly: true },
  },
  {
    path: "/verify-email",
    name: "verify-email",
    component: VerifyEmailView,
  },
  {
    path: "/resend-verification",
    name: "resend-verification",
    component: VerifyEmailView,
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
    meta: { requiresAuth: true },
  },
  {
    path: "/sandbox",
    name: "sandbox",
    component: SandboxView,
    meta: { requiresAuth: true },
  },
  {
    path: "/settings",
    name: "settings",
    component: SettingsView,
    meta: { requiresAuth: true },
  },
  {
    path: "/admin",
    name: "admin",
    component: AdminView,
    meta: { requiresAdmin: true },
  },
  {
    path: "/be-a-mentor",
    name: "be-a-mentor",
    component: BeAMentorView,
    meta: { requiresAuth: true },
  }
]


const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes,
})

// Uwaga: to tylko wygoda dla użytkownika (żeby nie widział pustych stron).
// Prawdziwa ochrona danych jest na backendzie ([Authorize]).
router.beforeEach((to) => {
  const token = localStorage.getItem('token')
  const role = localStorage.getItem('role')?.toLowerCase()

  // Zalogowany użytkownik nie musi widzieć logowania/rejestracji - od razu do jego panelu.
  if (to.meta.guestOnly && token) {
    return getHomeRouteForRole(role)
  }

  const needsLogin = to.meta.requiresAuth || to.meta.requiresCourseRole || to.meta.requiresAdmin
  if (needsLogin && !token) {
    return { name: 'login', query: { redirect: to.fullPath } }
  }

  if (to.meta.requiresAdmin && role !== 'admin') {
    return getHomeRouteForRole(role)
  }

  if (to.meta.requiresCourseRole && role !== 'admin' && role !== 'mentor') {
    return getHomeRouteForRole(role)
  }

  return true
})

export default router