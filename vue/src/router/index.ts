import { createRouter, createWebHistory } from 'vue-router'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/pages/LoginPage.vue'),
    },
    {
      path: '/',
      name: 'dashboard',
      component: () => import('@/pages/DashboardPage.vue'),
    },
    {
      path: '/campuses',
      name: 'campuses',
      component: () => import('@/pages/CampusesPage.vue'),
    },
    {
      path: '/faculties',
      name: 'faculties',
      component: () => import('@/pages/FacultiesPage.vue'),
    },
    {
      path: '/room-quotas',
      name: 'room-quotas',
      component: () => import('@/pages/RoomQuotasPage.vue'),
    },
    {
      path: '/allocation',
      name: 'allocation',
      component: () => import('@/pages/AllocationPeriodPage.vue'),
    },
    {
      path: '/students',
      name: 'students',
      component: () => import('@/pages/StudentsPage.vue'),
    },
    {
      path: '/preferences',
      name: 'preferences',
      component: () => import('@/pages/PreferencesPage.vue'),
    },
    {
      path: '/room-assignment',
      name: 'room-assignment',
      component: () => import('@/pages/RoomAssignmentPage.vue'),
    },
    {
      path: '/confirmations',
      name: 'confirmations',
      component: () => import('@/pages/ConfirmationsPage.vue'),
    },
    {
      path: '/settings',
      name: 'settings',
      component: () => import('@/pages/SettingsPage.vue'),
    },
  ],
})

export { router }
