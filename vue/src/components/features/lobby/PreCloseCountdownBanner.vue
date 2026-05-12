<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { AlertTriangle, AlertOctagon } from 'lucide-vue-next'
import { getPeriodCountdown } from '@/api/rooms'
import type { PeriodCountdownDto } from '@/api/rooms'

const countdown = ref<PeriodCountdownDto | null>(null)

const severity = computed<'low' | 'medium' | 'high' | null>(() => {
  const h = countdown.value?.hoursRemaining
  if (h === undefined || h === null) return null
  if (h > 72) return null
  if (h > 48) return 'low'
  if (h > 24) return 'medium'
  return 'high'
})

const hoursDisplay = computed(() => {
  if (!countdown.value) return ''
  const total = Math.max(0, countdown.value.hoursRemaining)
  const h = Math.floor(total)
  const m = Math.round((total % 1) * 60)
  return `${h}h ${m.toString().padStart(2, '0')}m`
})

const tone = computed(() => {
  switch (severity.value) {
    case 'low':
      return 'bg-amber-500/10 border-amber-500/30 text-amber-700 dark:text-amber-300'
    case 'medium':
      return 'bg-amber-500/15 border-amber-500/40 text-amber-800 dark:text-amber-200'
    case 'high':
      return 'bg-destructive/10 border-destructive/40 text-destructive dark:text-red-300'
    default:
      return ''
  }
})

async function load() {
  try {
    countdown.value = await getPeriodCountdown()
  } catch {
    // silently ignore
  }
}

let timer: ReturnType<typeof setInterval> | null = null

onMounted(() => {
  load()
  timer = setInterval(load, 5 * 60 * 1000)
})

onUnmounted(() => {
  if (timer !== null) clearInterval(timer)
})
</script>

<template>
  <aside
    v-if="severity"
    :class="['flex items-center gap-3 border-b px-4 py-2.5', tone]"
    role="status"
    aria-live="polite"
  >
    <span class="relative flex size-4 shrink-0" aria-hidden="true">
      <span
        v-if="severity === 'high'"
        class="absolute inline-flex size-full animate-ping rounded-full bg-destructive/40"
      />
      <AlertOctagon v-if="severity === 'high'" class="relative size-4" />
      <AlertTriangle v-else class="relative size-4" />
    </span>

    <span class="text-[13px] font-medium">
      <template v-if="severity === 'high'">Allocation period closing</template>
      <template v-else>Allocation period closes</template>
      in
      <strong class="font-mono tabular-nums">{{ hoursDisplay }}</strong>
    </span>

    <span class="hidden text-xs opacity-70 sm:inline">
      &middot; Unplaced students will be forfeited.
    </span>
  </aside>
</template>
