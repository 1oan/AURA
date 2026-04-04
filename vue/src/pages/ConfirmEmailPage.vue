<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { toast } from 'vue-sonner'
import AuthLayout from '@/components/layout/AuthLayout.vue'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth'
import { confirmEmail, resendConfirmation } from '@/api/auth'
import { ApiError } from '@/api/client'
import { Mail } from 'lucide-vue-next'
import { useRouter } from 'vue-router'

const authStore = useAuthStore()
const router = useRouter()

const digits = ref<string[]>(['', '', '', '', '', ''])
const inputs = ref<HTMLInputElement[]>([])
const error = ref('')
const isSubmitting = ref(false)
const resendCooldown = ref(0)

let cooldownInterval: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  if (authStore.isEmailConfirmed) {
    router.replace({ name: 'dashboard' })
    return
  }
  startCooldown(60)
  nextTick(() => inputs.value[0]?.focus())
})

function startCooldown(seconds: number) {
  resendCooldown.value = seconds
  if (cooldownInterval) clearInterval(cooldownInterval)
  cooldownInterval = setInterval(() => {
    resendCooldown.value--
    if (resendCooldown.value <= 0 && cooldownInterval) {
      clearInterval(cooldownInterval)
      cooldownInterval = null
    }
  }, 1000)
}

function handleInput(index: number, event: Event) {
  const target = event.target as HTMLInputElement
  const value = target.value.replace(/\D/g, '')

  if (value.length > 1) {
    // Handle paste: distribute digits across inputs
    const chars = value.split('').slice(0, 6 - index)
    chars.forEach((char, i) => {
      if (index + i < 6) digits.value[index + i] = char
    })
    const nextIndex = Math.min(index + chars.length, 5)
    nextTick(() => inputs.value[nextIndex]?.focus())
    if (index + chars.length >= 6) submitCode()
    return
  }

  digits.value[index] = value
  if (value && index < 5) {
    nextTick(() => inputs.value[index + 1]?.focus())
  }
  if (value && index === 5) submitCode()
}

function handleKeydown(index: number, event: KeyboardEvent) {
  if (event.key === 'Backspace' && !digits.value[index] && index > 0) {
    nextTick(() => inputs.value[index - 1]?.focus())
  }
}

function handlePaste(event: ClipboardEvent) {
  event.preventDefault()
  const text = event.clipboardData?.getData('text')?.replace(/\D/g, '') ?? ''
  if (!text) return
  const chars = text.split('').slice(0, 6)
  chars.forEach((char, i) => { digits.value[i] = char })
  const nextIndex = Math.min(chars.length, 5)
  nextTick(() => inputs.value[nextIndex]?.focus())
  if (chars.length >= 6) submitCode()
}

async function submitCode() {
  const code = digits.value.join('')
  if (code.length !== 6) return

  error.value = ''
  isSubmitting.value = true
  try {
    await confirmEmail(code)
    toast.success('Email confirmed!')
    await authStore.fetchCurrentUser()
    router.push({ name: 'dashboard' })
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Invalid or expired code.'
    } else {
      error.value = 'An unexpected error occurred.'
    }
    digits.value = ['', '', '', '', '', '']
    nextTick(() => inputs.value[0]?.focus())
  } finally {
    isSubmitting.value = false
  }
}

async function handleResend() {
  if (resendCooldown.value > 0) return
  error.value = ''
  try {
    await resendConfirmation()
    toast.success('A new code has been sent to your email.')
    startCooldown(60)
    digits.value = ['', '', '', '', '', '']
    nextTick(() => inputs.value[0]?.focus())
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Failed to resend code.'
    }
  }
}

const userEmail = authStore.user?.email ?? ''
</script>

<template>
  <AuthLayout>
    <div class="space-y-6">
      <div class="flex flex-col items-center space-y-3 text-center">
        <div class="flex size-12 items-center justify-center rounded-full bg-primary/10">
          <Mail class="size-6 text-primary" />
        </div>
        <div class="space-y-1">
          <h1 class="text-2xl font-semibold tracking-tight">Check your email</h1>
          <p class="text-sm text-muted-foreground">
            We sent a 6-digit code to
            <span class="font-medium text-foreground">{{ userEmail }}</span>
          </p>
        </div>
      </div>

      <div class="space-y-4">
        <div v-if="error" role="alert" class="rounded-md bg-destructive/10 p-3 text-center text-sm text-destructive">
          {{ error }}
        </div>

        <!-- OTP Input -->
        <div class="flex justify-center gap-2" @paste="handlePaste">
          <input
            v-for="(_, index) in digits"
            :key="index"
            :ref="(el) => { if (el) inputs[index] = el as HTMLInputElement }"
            v-model="digits[index]"
            type="text"
            inputmode="numeric"
            maxlength="6"
            class="size-12 rounded-md border bg-background text-center text-lg font-semibold transition-colors focus:border-primary focus:outline-none focus:ring-2 focus:ring-primary/20"
            :class="{ 'border-destructive': error }"
            :disabled="isSubmitting"
            @input="handleInput(index, $event)"
            @keydown="handleKeydown(index, $event)"
          />
        </div>

        <Button class="w-full" :disabled="isSubmitting || digits.join('').length < 6" @click="submitCode">
          {{ isSubmitting ? 'Verifying...' : 'Verify email' }}
        </Button>

        <p class="text-center text-sm text-muted-foreground">
          Didn't receive the code?
          <button
            class="font-medium transition-colors"
            :class="resendCooldown > 0 ? 'text-muted-foreground/50 cursor-not-allowed' : 'text-primary hover:underline cursor-pointer'"
            :disabled="resendCooldown > 0"
            @click="handleResend"
          >
            {{ resendCooldown > 0 ? `Resend in ${resendCooldown}s` : 'Resend code' }}
          </button>
        </p>
      </div>
    </div>
  </AuthLayout>
</template>