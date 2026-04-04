import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/LoginPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/register',
      name: 'register',
      component: () => import('@/pages/RegisterPage.vue'),
      meta: { guest: true },
    },
    {
      path: '/confirm-email',
      name: 'confirm-email',
      component: () => import('@/pages/ConfirmEmailPage.vue'),
      meta: { requiresAuth: true, skipEmailCheck: true },
    },
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/pages/DashboardPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/campuses',
      name: 'campuses',
      component: () => import('@/pages/CampusesPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/faculties',
      name: 'faculties',
      component: () => import('@/pages/FacultiesPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/room-quotas',
      name: 'room-quotas',
      component: () => import('@/pages/RoomQuotasPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/allocation',
      name: 'allocation',
      component: () => import('@/pages/AllocationPeriodPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/students',
      name: 'students',
      component: () => import('@/pages/StudentsPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/preferences',
      name: 'preferences',
      component: () => import('@/pages/PreferencesPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/room-assignment',
      name: 'room-assignment',
      component: () => import('@/pages/RoomAssignmentPage.vue'),
      meta: { requiresAuth: true, roles: ['SuperAdmin', 'FacultyAdmin'] },
    },
    {
      path: '/confirmations',
      name: 'confirmations',
      component: () => import('@/pages/ConfirmationsPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/pages/SettingsPage.vue'),
      meta: { requiresAuth: true },
    },
  ],
})

router.beforeEach(async (to) => {
  const token = localStorage.getItem('auth_token')

  if (to.meta.requiresAuth && !token) {
    return { name: 'login' }
  }

  if (to.meta.guest && token) {
    return { name: 'dashboard' }
  }

  if (to.meta.requiresAuth && token) {
    const authStore = useAuthStore()
    if (!authStore.user) {
      await authStore.fetchCurrentUser()
    }

    // Redirect unconfirmed users to confirm-email page
    if (!to.meta.skipEmailCheck && !authStore.isEmailConfirmed) {
      return { name: 'confirm-email' }
    }

    // Role-based access control
    if (to.meta.roles) {
      const userRole = authStore.user?.role
      if (!userRole || !(to.meta.roles as string[]).includes(userRole)) {
        return { name: 'dashboard' }
      }
    }
  }
})

export { router }