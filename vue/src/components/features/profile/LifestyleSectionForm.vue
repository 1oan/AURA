<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { PhCircleNotch as CircleNotch } from '@phosphor-icons/vue'
import { ApiError } from '@/api/client'
import { submitLifestyle } from '@/api/profile'
import type { LifestyleDto, SubmitLifestyleRequest } from '@/api/profile'

interface Props {
  initial: LifestyleDto | null
}

const props = defineProps<Props>()
const emit = defineEmits<{ saved: [] }>()

// The backend DTO types these as plain strings, but the server only emits the
// enum names below. Narrow the cast rather than using `as any`.
type Sleep = SubmitLifestyleRequest['sleepSchedule']
type Wake = SubmitLifestyleRequest['wakeUpTime']
type Noise = SubmitLifestyleRequest['noiseTolerance']
type Study = SubmitLifestyleRequest['studyLocation']
type Guests = SubmitLifestyleRequest['guestFrequency']
type Smoking = SubmitLifestyleRequest['smokingHabit']

const sleep = ref<Sleep>((props.initial?.sleepSchedule as Sleep) ?? 'Normal')
const wake = ref<Wake>((props.initial?.wakeUpTime as Wake) ?? 'Normal')
const cleanliness = ref<number>(props.initial?.cleanlinessLevel ?? 3)
const noise = ref<Noise>((props.initial?.noiseTolerance as Noise) ?? 'Some')
const study = ref<Study>((props.initial?.studyLocation as Study) ?? 'Mixed')
const guests = ref<Guests>((props.initial?.guestFrequency as Guests) ?? 'Weekly')
const smoking = ref<Smoking>((props.initial?.smokingHabit as Smoking) ?? 'No')

const saving = ref(false)
const error = ref('')

const canSave = computed(() => !saving.value)

async function handleSave() {
  saving.value = true
  error.value = ''
  try {
    await submitLifestyle({
      sleepSchedule: sleep.value,
      wakeUpTime: wake.value,
      cleanlinessLevel: cleanliness.value,
      noiseTolerance: noise.value,
      studyLocation: study.value,
      guestFrequency: guests.value,
      smokingHabit: smoking.value,
    })
    toast.success('Lifestyle saved.')
    emit('saved')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not save lifestyle.'
    } else {
      error.value = 'Could not save lifestyle.'
    }
  } finally {
    saving.value = false
  }
}

const sleepOptions: readonly { value: Sleep; label: string }[] = [
  { value: 'Early', label: 'Before 10pm' },
  { value: 'Normal', label: '10pm – 12am' },
  { value: 'Late', label: 'After midnight' },
]
const wakeOptions: readonly { value: Wake; label: string }[] = [
  { value: 'Early', label: 'Before 7am' },
  { value: 'Normal', label: '7am – 9am' },
  { value: 'Late', label: 'After 9am' },
]
const noiseOptions: readonly { value: Noise; label: string }[] = [
  { value: 'Silent', label: 'I need silence' },
  { value: 'Some', label: 'Some background is fine' },
  { value: 'Any', label: 'Doesn’t bother me' },
]
const studyOptions: readonly { value: Study; label: string }[] = [
  { value: 'Room', label: 'In my room' },
  { value: 'Campus', label: 'Library / campus' },
  { value: 'Mixed', label: 'A mix' },
]
const guestOptions: readonly { value: Guests; label: string }[] = [
  { value: 'Rarely', label: 'Rarely' },
  { value: 'Weekly', label: 'A few times a week' },
  { value: 'Daily', label: 'Most days' },
]
const smokingOptions: readonly { value: Smoking; label: string }[] = [
  { value: 'No', label: 'No' },
  { value: 'OutdoorsOnly', label: 'Outdoors only' },
  { value: 'Yes', label: 'Yes' },
]

const cleanlinessHint = computed(() => {
  const labels = ['Very tidy', 'Tidy', 'Balanced', 'Relaxed', 'Very relaxed']
  return labels[cleanliness.value - 1] ?? ''
})
</script>

<template>
  <form class="space-y-6" @submit.prevent="handleSave">
    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">Sleep schedule</legend>
      <div role="radiogroup" aria-label="Sleep schedule" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in sleepOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="sleep === opt.value"
          :variant="sleep === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="sleep = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">Wake-up time</legend>
      <div role="radiogroup" aria-label="Wake-up time" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in wakeOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="wake === opt.value"
          :variant="wake === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="wake = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="flex items-baseline justify-between gap-2 text-[13px] font-medium text-foreground">
        <span>Cleanliness</span>
        <span class="text-xs font-normal text-muted-foreground">{{ cleanlinessHint }}</span>
      </legend>
      <div
        role="radiogroup"
        aria-label="Cleanliness level"
        class="inline-flex items-center gap-0.5 rounded-lg border bg-background p-1"
      >
        <button
          v-for="n in 5"
          :key="n"
          type="button"
          role="radio"
          :aria-checked="cleanliness === n"
          :aria-label="`Level ${n} of 5`"
          class="relative grid size-8 place-items-center rounded-md font-mono text-xs tabular-nums transition-colors duration-200 hover:bg-muted/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring active:scale-[0.95]"
          :class="cleanliness === n ? 'bg-slate-500 text-white shadow-sm' : 'text-muted-foreground'"
          @click="cleanliness = n"
        >
          {{ n }}
        </button>
      </div>
      <p class="text-xs text-muted-foreground">1 is the tidiest, 5 is the most relaxed.</p>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">Noise tolerance</legend>
      <div role="radiogroup" aria-label="Noise tolerance" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in noiseOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="noise === opt.value"
          :variant="noise === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="noise = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">Where I study</legend>
      <div role="radiogroup" aria-label="Where I study" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in studyOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="study === opt.value"
          :variant="study === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="study = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">How often I have guests</legend>
      <div role="radiogroup" aria-label="Guest frequency" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in guestOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="guests === opt.value"
          :variant="guests === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="guests = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <fieldset class="space-y-2">
      <legend class="text-[13px] font-medium text-foreground">Smoking</legend>
      <div role="radiogroup" aria-label="Smoking habit" class="flex flex-wrap gap-1.5">
        <Button
          v-for="opt in smokingOptions"
          :key="opt.value"
          type="button"
          role="radio"
          :aria-checked="smoking === opt.value"
          :variant="smoking === opt.value ? 'default' : 'outline'"
          size="sm"
          class="h-8 text-xs"
          @click="smoking = opt.value"
        >
          {{ opt.label }}
        </Button>
      </div>
    </fieldset>

    <p v-if="error" class="text-xs text-destructive" role="alert">{{ error }}</p>

    <Button
      type="submit"
      class="w-full transition-transform active:scale-[0.98]"
      :disabled="!canSave"
    >
      <CircleNotch v-if="saving" :size="16" class="mr-2 animate-spin" />
      {{ saving ? 'Saving…' : 'Save lifestyle' }}
    </Button>
  </form>
</template>
