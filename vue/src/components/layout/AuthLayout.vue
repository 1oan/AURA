<script setup lang="ts">
import { Moon, Sun, Monitor } from 'lucide-vue-next'
import { useTheme } from '@/composables/useTheme'

const { theme } = useTheme()
</script>

<template>
  <div class="grid min-h-screen lg:grid-cols-2">
    <!-- Brand panel -->
    <div class="relative hidden flex-col items-center justify-center overflow-hidden bg-primary p-10 lg:flex">
      <div class="absolute inset-0 bg-gradient-to-br from-primary via-primary to-primary/80" />
      <div
        class="absolute inset-0 opacity-[0.03]"
        :style="{
          backgroundImage: 'radial-gradient(circle at 1px 1px, currentColor 1px, transparent 0)',
          backgroundSize: '24px 24px',
        }"
      />

      <div class="relative flex flex-col items-center space-y-6 text-center">
        <div class="flex size-16 items-center justify-center rounded-2xl bg-white/20 text-2xl font-bold text-white backdrop-blur-sm">
          A
        </div>
        <div class="space-y-3">
          <h1 class="text-3xl font-bold tracking-tight text-white">AURA</h1>
          <p class="text-sm font-medium uppercase tracking-widest text-white/60">
            Automated University Room Allocation
          </p>
        </div>
        <p class="max-w-xs text-sm leading-relaxed text-white/50">
          From preferences to placement, all in one platform.
        </p>
      </div>
    </div>

    <!-- Form panel -->
    <div class="flex flex-col">
      <div class="flex items-center justify-between p-4 lg:justify-end">
        <div class="flex items-center gap-2 lg:hidden">
          <div class="flex size-8 items-center justify-center rounded-lg bg-primary text-sm font-bold text-primary-foreground">
            A
          </div>
          <span class="font-semibold">AURA</span>
        </div>
        <div class="flex items-center gap-0.5 rounded-lg bg-muted p-0.5">
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-md transition-all duration-200',
              theme === 'light' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="Light"
            @click="theme = 'light'"
          >
            <Sun class="size-3.5" />
          </button>
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-md transition-all duration-200',
              theme === 'dark' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="Dark"
            @click="theme = 'dark'"
          >
            <Moon class="size-3.5" />
          </button>
          <button
            :class="[
              'flex size-7 items-center justify-center rounded-md transition-all duration-200',
              theme === 'system' ? 'bg-background text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground',
            ]"
            title="System"
            @click="theme = 'system'"
          >
            <Monitor class="size-3.5" />
          </button>
        </div>
      </div>
      <div class="flex flex-1 items-center justify-center px-4 pb-8">
        <div class="w-full max-w-sm">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>
