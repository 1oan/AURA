<script setup lang="ts">
import { ref } from 'vue'
import AuthLayout from '@/components/layout/AuthLayout.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useAuthStore } from '@/stores/auth'
import { ApiError } from '@/api/client'

const authStore = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const confirmPassword = ref('')
const error = ref('')
const isSubmitting = ref(false)

async function handleSubmit() {
  error.value = ''

  if (password.value !== confirmPassword.value) {
    error.value = 'Passwords do not match.'
    return
  }

  isSubmitting.value = true

  try {
    await authStore.register({
      firstName: firstName.value,
      lastName: lastName.value,
      email: email.value,
      password: password.value,
    })
  } catch (e) {
    if (e instanceof ApiError && e.status === 400) {
      const data = e.data as { detail?: string; errors?: Record<string, string[]> }
      if (data.errors) {
        error.value = Object.values(data.errors).flat().join(' ')
      } else {
        error.value = data.detail ?? 'Registration failed.'
      }
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
      <div class="space-y-2">
        <h1 class="text-2xl font-semibold tracking-tight">Create an account</h1>
        <p class="text-sm text-muted-foreground">
          Enter your details to get started.
        </p>
      </div>

      <form class="space-y-4" @submit.prevent="handleSubmit">
        <div v-if="error" class="rounded-md bg-destructive/10 p-3 text-sm text-destructive">
          {{ error }}
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <label class="text-sm font-medium" for="firstName">First name</label>
            <Input
              id="firstName"
              v-model="firstName"
              type="text"
              placeholder="John"
              required
            />
          </div>
          <div class="space-y-2">
            <label class="text-sm font-medium" for="lastName">Last name</label>
            <Input
              id="lastName"
              v-model="lastName"
              type="text"
              placeholder="Doe"
              required
            />
          </div>
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
            placeholder="At least 8 characters"
            required
            minlength="8"
          />
        </div>

        <div class="space-y-2">
          <label class="text-sm font-medium" for="confirmPassword">Confirm password</label>
          <Input
            id="confirmPassword"
            v-model="confirmPassword"
            type="password"
            placeholder="Repeat your password"
            required
            minlength="8"
          />
        </div>

        <Button class="w-full" type="submit" :disabled="isSubmitting">
          {{ isSubmitting ? 'Creating account...' : 'Create account' }}
        </Button>
      </form>

      <p class="text-center text-sm text-muted-foreground">
        Already have an account?
        <RouterLink to="/login" class="text-primary underline-offset-4 hover:underline">
          Sign in
        </RouterLink>
      </p>
    </div>
  </AuthLayout>
</template>
