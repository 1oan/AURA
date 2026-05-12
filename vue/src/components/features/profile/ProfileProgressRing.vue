<script setup lang="ts">
import { computed, toRef } from 'vue'
import { useTransition, TransitionPresets } from '@vueuse/core'

interface Props {
  value: number
}

const props = defineProps<Props>()

// Animate the displayed value from whatever it was to the new prop value.
// useTransition gives spring-feel ease for cheap on top of a reactive source.
const animated = useTransition(toRef(props, 'value'), {
  duration: 700,
  transition: TransitionPresets.easeOutCubic,
})

const displayed = computed(() => Math.round(animated.value))

// Stroke geometry. r=18 → circumference ≈ 113.097.
// dashoffset is computed live from the animated value so the arc fills in sync.
const CIRC = 2 * Math.PI * 18
const dashOffset = computed(() => CIRC - (CIRC * animated.value) / 100)
</script>

<template>
  <div class="relative inline-flex size-12 shrink-0 items-center justify-center">
    <svg viewBox="0 0 44 44" class="size-12 -rotate-90 overflow-visible">
      <circle
        cx="22" cy="22" r="18"
        fill="none"
        stroke="currentColor"
        stroke-width="3"
        class="text-muted/60"
      />
      <circle
        cx="22" cy="22" r="18"
        fill="none"
        stroke="currentColor"
        stroke-width="3"
        stroke-linecap="round"
        :stroke-dasharray="CIRC"
        :stroke-dashoffset="dashOffset"
        class="text-primary transition-colors duration-300"
      />
    </svg>
    <span class="absolute font-mono text-[11px] font-semibold tabular-nums text-foreground">
      {{ displayed }}
    </span>
  </div>
</template>
