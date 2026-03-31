<script setup lang="ts">
import { ref } from 'vue'
import AuthLayout from '@/components/layout/AuthLayout.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useAuthStore } from '@/stores/auth'
import { ApiError } from '@/api/client'

const authStore = useAuthStore()

const email = ref('')
const password = ref('')
const error = ref('')
const isSubmitting = ref(false)

async function handleSubmit() {
  error.value = ''
  isSubmitting.value = true

  try {
    await authStore.login({
      email: email.value,
      password: password.value,
    })
  } catch (e) {
    if (e instanceof ApiError && e.status === 400) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Invalid email or password.'
    } else {
      error.value = 'An unexpected error occurred.'
    }
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <AuthLayout>
    <div class="space-y-6">
      <div class="space-y-1.5">
        <h1 class="text-xl font-semibold tracking-tight">Welcome back</h1>
        <p class="text-[13px] text-muted-foreground">
          Sign in to your account to continue.
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <div v-if="error" role="alert" class="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {{ error }}
        </div>

        <div class="space-y-2">
          <label class="text-sm font-medium" for="email">Email</label>
          <Input
            id="email"
            v-model="email"
            type="email"
            placeholder="name@uaic.ro"
            required
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-medium" for="password">Password</label>
          <Input
            id="password"
            v-model="password"
            type="password"
            placeholder="Enter your password"
            required
          />
        </div>

        <Button class="w-full" type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? 'Signing in...' : 'Sign in' }}
        </Button>
      </form>

      <p class="text-center text-sm text-muted-foreground">
        Don't have an account?
        <RouterLink to="/register" class="text-primary underline-offset-4 hover:underline">
          Create one
        </RouterLink>
      </p>
    </div>
  </AuthLayout>
</template>
