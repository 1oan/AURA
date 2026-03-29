<script setup lang="ts">
import { RouterView } from 'vue-router'
import { Toaster } from 'vue-sonner'
import { useTheme } from '@/composables/useTheme'
import { useAuthStore } from '@/stores/auth'

useTheme()

const authStore = useAuthStore()
authStore.fetchCurrentUser()
</script>

<template>
  <div>
    <Toaster position="bottom-right" :expand="true" rich-colors />
    <RouterView />
  </div>
</template>