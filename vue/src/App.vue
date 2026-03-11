<script setup lang="ts">
import { RouterView } from 'vue-router'
import { useTheme } from '@/composables/useTheme'
import { useAuthStore } from '@/stores/auth'

useTheme()

const authStore = useAuthStore()
authStore.fetchCurrentUser()
</script>

<template>
  <RouterView />
</template>