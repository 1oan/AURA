<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTransition, TransitionPresets } from '@vueuse/core'
import { PhArrowRight as ArrowRight, PhX as X } from '@phosphor-icons/vue'
import { getMyProfile } from '@/api/profile'

const DISMISS_KEY = 'profile-banner-dismissed'
const DISMISS_WINDOW_MS = 7 * 24 * 60 * 60 * 1000

const router = useRouter()
const completeness = ref<number | null>(null)
const dismissed = ref(false)

const visible = computed(
  () => completeness.value !== null && completeness.value < 100 && !dismissed.value,
)

// Smoothly tween the displayed value once the API call resolves.
const animated = useTransition(() => completeness.value ?? 0, {
  duration: 600,
  transition: TransitionPresets.easeOutCubic,
})
const displayed = computed(() => Math.round(animated.value))

const CIRC = 2 * Math.PI * 10 // r=10, circumference ≈ 62.83
const dashOffset = computed(() => CIRC - (CIRC * animated.value) / 100)

onMounted(async () => {
  const dismissedAt = localStorage.getItem(DISMISS_KEY)
  if (dismissedAt) {
    const ts = Number(dismissedAt)
    if (!Number.isNaN(ts) && Date.now() - ts < DISMISS_WINDOW_MS) {
      dismissed.value = true
      return
    }
    localStorage.removeItem(DISMISS_KEY)
  }
  try {
    const profile = await getMyProfile()
    completeness.value = profile.completenessPercent
  } catch {
    // Silent: banner just won't show.
  }
})

function dismiss() {
  localStorage.setItem(DISMISS_KEY, String(Date.now()))
  dismissed.value = true
}

function navigate() {
  router.push('/profile')
}
</script>

<template>
  <div
    v-if="visible"
    class="group relative flex items-center gap-3 rounded-lg border border-primary/15 bg-primary/[0.04] py-2.5 pl-2.5 pr-3 transition-colors hover:bg-primary/[0.07]"
  >
    <button
      type="button"
      class="flex flex-1 items-center gap-3 text-left focus-visible:outline-none"
      @click="navigate"
    >
      <span class="relative inline-flex size-7 shrink-0 items-center justify-center">
        <svg viewBox="0 0 24 24" class="size-7 -rotate-90">
          <circle cx="12" cy="12" r="10" fill="none" stroke="currentColor" stroke-width="2" class="text-primary/15" />
          <circle
            cx="12"
            cy="12"
            r="10"
            fill="none"
            stroke="currentColor"
            stroke-width="2"
            stroke-linecap="round"
            :stroke-dasharray="CIRC"
            :stroke-dashoffset="dashOffset"
            class="text-primary"
          />
        </svg>
        <span class="absolute font-mono text-[9px] font-semibold tabular-nums text-primary">
          {{ displayed }}
        </span>
      </span>
      <span class="flex-1 text-[13px]">
        Your profile is
        <span class="font-semibold tabular-nums">{{ displayed }}%</span>
        complete — better matches with more details
      </span>
      <ArrowRight
        :size="14"
        class="shrink-0 text-primary transition-transform duration-200 group-hover:translate-x-0.5"
      />
    </button>
    <button
      type="button"
      class="shrink-0 rounded p-1 text-muted-foreground transition-colors hover:bg-muted/60 hover:text-foreground"
      aria-label="Dismiss for 7 days"
      @click="dismiss"
    >
      <X :size="14" />
    </button>
  </div>
</template>
