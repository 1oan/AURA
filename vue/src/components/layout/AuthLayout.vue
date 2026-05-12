<script setup lang="ts">
import { Moon, Sun, Monitor } from 'lucide-vue-next'
import { useTheme } from '@/composables/useTheme'

const { theme } = useTheme()

const features = [
  { label: 'Points-based allocation', detail: 'Merit ranks students fairly within each faculty' },
  { label: 'Roommate matching', detail: 'Form groups with compatible classmates before rooms are assigned' },
  { label: 'Live preferences', detail: 'Submit and adjust your dormitory preferences any time during an open period' },
]
</script>

<template>
  <div class="grid min-h-dvh lg:grid-cols-[1fr_480px]">
    <!-- Brand panel -->
    <div
      class="grain relative hidden flex-col justify-between overflow-hidden bg-primary p-12 lg:flex"
    >
      <!-- Radial glow -->
      <div
        class="pointer-events-none absolute inset-0"
        style="background: radial-gradient(ellipse 80% 60% at 50% 60%, oklch(0.50 0.18 259 / 0.35) 0%, transparent 70%)"
      />

      <!-- Concentric decorative rings -->
      <div
        class="pointer-events-none absolute left-1/2 top-1/2 -translate-x-1/2 -translate-y-1/2"
        aria-hidden="true"
      >
        <div class="absolute -translate-x-1/2 -translate-y-1/2 size-[520px] rounded-full border border-white/[0.06]" />
        <div class="absolute -translate-x-1/2 -translate-y-1/2 size-[380px] rounded-full border border-white/[0.08]" />
        <div class="absolute -translate-x-1/2 -translate-y-1/2 size-[240px] rounded-full border border-white/[0.11]" />
        <div class="absolute -translate-x-1/2 -translate-y-1/2 size-[110px] rounded-full border border-white/[0.15]" />
      </div>

      <!-- Top: wordmark -->
      <div class="relative flex items-center gap-3">
        <div class="flex size-8 items-center justify-center rounded-lg bg-white/15 font-bold text-white backdrop-blur-sm ring-1 ring-white/20 text-sm">
          A
        </div>
        <span class="text-sm font-semibold tracking-wide text-white/90">AURA</span>
      </div>

      <!-- Center: headline + features -->
      <div class="relative space-y-10">
        <div class="space-y-3">
          <p class="text-[11px] font-semibold uppercase tracking-[0.2em] text-white/40">
            Alexandru Ioan Cuza University
          </p>
          <h1 class="text-5xl font-bold leading-[1.05] tracking-tight text-white">
            Student housing,<br>done right.
          </h1>
          <p class="max-w-xs text-[15px] leading-relaxed text-white/60">
            One place to rank your preferences, form roommate groups, and get assigned a room.
          </p>
        </div>

        <ul class="space-y-4">
          <li
            v-for="f in features"
            :key="f.label"
            class="flex items-start gap-3"
          >
            <span class="mt-[3px] flex size-4 shrink-0 items-center justify-center rounded-full bg-white/15">
              <span class="size-1.5 rounded-full bg-white/80" />
            </span>
            <div>
              <p class="text-[13px] font-medium text-white/85">{{ f.label }}</p>
              <p class="text-[12px] text-white/45">{{ f.detail }}</p>
            </div>
          </li>
        </ul>
      </div>

      <!-- Bottom: legal links -->
      <div class="relative flex items-center gap-4">
        <span class="text-[11px] text-white/30">© {{ new Date().getFullYear() }} AURA</span>
      </div>
    </div>

    <!-- Form panel -->
    <div class="flex flex-col bg-background">
      <div class="flex items-center justify-between p-4 lg:justify-end">
        <!-- Mobile wordmark -->
        <div class="flex items-center gap-2 lg:hidden">
          <div class="flex size-7 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            A
          </div>
          <span class="text-sm font-semibold tracking-tight">AURA</span>
        </div>
        <!-- Theme switcher -->
        <div class="flex items-center gap-0.5 rounded-md bg-muted p-0.5">
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-sm transition-all duration-150',
              theme === 'light' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="Light"
            @click="theme = 'light'"
          >
            <Sun class="size-3.5" />
          </button>
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-sm transition-all duration-150',
              theme === 'dark' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="Dark"
            @click="theme = 'dark'"
          >
            <Moon class="size-3.5" />
          </button>
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-sm transition-all duration-150',
              theme === 'system' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="System"
            @click="theme = 'system'"
          >
            <Monitor class="size-3.5" />
          </button>
        </div>
      </div>
      <div class="flex flex-1 items-center justify-center px-6 pb-10">
        <div class="w-full max-w-[360px]">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>
