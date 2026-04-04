import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { router } from '@/router'
import * as authApi from '@/api/auth'

export const useAuthStore = defineStore('auth', () => {
  const user = ref<authApi.UserDto | null>(null)
  const token = ref<string | null>(localStorage.getItem('auth_token'))
  const isLoading = ref(false)

  const isAuthenticated = computed(() => !!token.value)
  const isEmailConfirmed = computed(() => user.value?.isEmailConfirmed ?? false)

  function setAuth(result: authApi.AuthResult) {
    token.value = result.token
    localStorage.setItem('auth_token', result.token)

    user.value = {
      id: result.userId,
      email: result.email,
      firstName: result.firstName,
      lastName: result.lastName,
      role: result.role,
      matriculationCode: null,
      createdAt: new Date().toISOString(),
      isEmailConfirmed: result.isEmailConfirmed,
    }
  }

  async function register(data: authApi.RegisterRequest) {
    const result = await authApi.register(data)
    setAuth(result)
    router.push({ name: 'confirm-email' })
  }

  async function login(data: authApi.LoginRequest) {
    const result = await authApi.login(data)
    setAuth(result)
    if (result.isEmailConfirmed) {
      router.push({ name: 'dashboard' })
    } else {
      router.push({ name: 'confirm-email' })
    }
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('auth_token')
    router.push({ name: 'login' })
  }

  async function fetchCurrentUser() {
    if (!token.value) return

    try {
      isLoading.value = true
      user.value = await authApi.getCurrentUser()
    } catch {
      logout()
    } finally {
      isLoading.value = false
    }
  }

  return {
    user,
    token,
    isLoading,
    isAuthenticated,
    isEmailConfirmed,
    register,
    login,
    logout,
    fetchCurrentUser,
  }
})