<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { PhCaretLeft as CaretLeft, PhCircleNotch as CircleNotch } from '@phosphor-icons/vue'
import { ApiError } from '@/api/client'
import { submitTipi } from '@/api/profile'

const emit = defineEmits<{ saved: [] }>()

const items = [
  'extraverted, enthusiastic.',
  'critical, quarrelsome.',
  'dependable, self-disciplined.',
  'anxious, easily upset.',
  'open to new experiences, complex.',
  'reserved, quiet.',
  'sympathetic, warm.',
  'disorganized, careless.',
  'calm, emotionally stable.',
  'conventional, uncreative.',
]

const answers = ref<(number | null)[]>(Array(10).fill(null))
const currentIndex = ref(0)
const saving = ref(false)
const error = ref('')

const isLast = computed(() => currentIndex.value === items.length - 1)
const allAnswered = computed(() => answers.value.every(a => a !== null))

function selectAnswer(value: number) {
  answers.value[currentIndex.value] = value
  if (!isLast.value) {
    setTimeout(() => {
      currentIndex.value = Math.min(currentIndex.value + 1, items.length - 1)
    }, 200)
  }
}

function goBack() {
  if (currentIndex.value > 0) currentIndex.value -= 1
}

async function handleSubmit() {
  if (!allAnswered.value) return
  saving.value = true
  error.value = ''
  try {
    await submitTipi(answers.value as number[])
    toast.success('Personality saved.')
    emit('saved')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not save personality.'
    } else {
      error.value = 'Could not save personality.'
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center gap-1 pt-1">
      <span
        v-for="(_, i) in items"
        :key="i"
        class="h-1 flex-1 rounded-full transition-colors duration-300"
        :class="answers[i] !== null ? 'bg-violet-500' : 'bg-muted'"
      />
    </div>

    <!-- key={currentIndex} forces the block to remount per question so the enter
         animation re-runs. animate-in classes come from tw-animate-css. -->
    <div
      :key="currentIndex"
      class="space-y-1 animate-in fade-in slide-in-from-right-2 duration-300"
    >
      <p class="text-xs font-medium text-muted-foreground">I see myself as…</p>
      <p class="text-balance text-xl font-semibold tracking-tight leading-snug">
        {{ items[currentIndex] }}
      </p>
    </div>

    <div class="grid grid-cols-7 gap-1.5">
      <Button
        v-for="value in 7"
        :key="value"
        type="button"
        :variant="answers[currentIndex] === value ? 'default' : 'outline'"
        class="h-12 text-sm font-mono tabular-nums transition-transform active:scale-[0.95]"
        @click="selectAnswer(value)"
      >
        {{ value }}
      </Button>
    </div>

    <div class="flex justify-between text-xs text-muted-foreground">
      <span>Disagree strongly</span>
      <span>Agree strongly</span>
    </div>

    <div class="flex items-center justify-between pt-1">
      <Button
        v-if="currentIndex > 0"
        variant="ghost"
        size="sm"
        class="h-8 text-xs"
        @click="goBack"
      >
        <CaretLeft :size="14" class="mr-1" />
        Back
      </Button>
      <span v-else class="text-xs text-muted-foreground">
        Question {{ currentIndex + 1 }} of {{ items.length }}
      </span>

      <Button
        v-if="isLast"
        type="button"
        size="sm"
        class="h-8 transition-transform active:scale-[0.98]"
        :disabled="!allAnswered || saving"
        @click="handleSubmit"
      >
        <CircleNotch v-if="saving" :size="14" class="mr-2 animate-spin" />
        {{ saving ? 'Saving…' : 'Submit' }}
      </Button>
    </div>

    <p v-if="error" class="text-xs text-destructive" role="alert">{{ error }}</p>
  </div>
</template>
