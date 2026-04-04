<script setup lang="ts">
import { computed } from 'vue'
import { RouterView } from 'vue-router'
import { Toaster } from 'vue-sonner'
import { useTheme } from '@/composables/useTheme'
import { useAuthStore } from '@/stores/auth'

const { theme } = useTheme()

const toasterTheme = computed(() => {
  if (theme.value === 'system') {
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
  }
  return theme.value
})

const authStore = useAuthStore()
authStore.fetchCurrentUser()
</script>

<template>
  <RouterView />
  <Toaster :theme="toasterTheme" position="bottom-right" rich-colors />
</template>