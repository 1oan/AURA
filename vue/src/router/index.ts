import { createRouter, createWebHistory } from 'vue-router'

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
      path: '/',
      name: 'dashboard',
      component: () => import('@/pages/DashboardPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/campuses',
      name: 'campuses',
      component: () => import('@/pages/CampusesPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/faculties',
      name: 'faculties',
      component: () => import('@/pages/FacultiesPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/room-quotas',
      name: 'room-quotas',
      component: () => import('@/pages/RoomQuotasPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/allocation',
      name: 'allocation',
      component: () => import('@/pages/AllocationPeriodPage.vue'),
      meta: { requiresAuth: true },
    },
    {
      path: '/students',
      name: 'students',
      component: () => import('@/pages/StudentsPage.vue'),
      meta: { requiresAuth: true },
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
      meta: { requiresAuth: true },
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

router.beforeEach((to) => {
  const token = localStorage.getItem('auth_token')

  if (to.meta.requiresAuth && !token) {
    return { name: 'login' }
  }

  if (to.meta.guest && token) {
    return { name: 'dashboard' }
  }
})

export { router }
