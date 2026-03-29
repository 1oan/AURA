<script setup lang="ts">
import { Moon, Sun, Monitor } from 'lucide-vue-next'
import { useTheme } from '@/composables/useTheme'

const { theme } = useTheme()
</script>

<template>
  <div class="grid min-h-screen lg:grid-cols-2">
    <!-- Brand panel -->
    <div class="relative hidden flex-col items-center justify-center overflow-hidden bg-primary lg:flex">
      <div
        class="absolute inset-0 opacity-[0.06]"
        :style="{
          backgroundImage: `
            linear-gradient(rgba(255,255,255,0.03) 1px, transparent 1px),
            linear-gradient(90deg, rgba(255,255,255,0.03) 1px, transparent 1px)
          `,
          backgroundSize: '32px 32px',
        }"
      />

      <div class="relative flex flex-col items-center space-y-8 text-center">
        <div class="flex size-14 items-center justify-center rounded-lg bg-white/15 text-xl font-bold text-white backdrop-blur-sm">
          A
        </div>
        <div class="space-y-2">
          <h1 class="text-2xl font-semibold tracking-tight text-white">AURA</h1>
          <p class="text-[11px] font-medium uppercase tracking-[0.2em] text-white/50">
            Automated University Room Allocation
          </p>
        </div>
      </div>
    </div>

    <!-- Form panel -->
    <div class="flex flex-col">
      <div class="flex items-center justify-between p-4 lg:justify-end">
        <div class="flex items-center gap-2 lg:hidden">
          <div class="flex size-7 items-center justify-center rounded-md bg-primary text-xs font-bold text-primary-foreground">
            A
          </div>
          <span class="text-sm font-semibold">AURA</span>
        </div>
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
      <div class="flex flex-1 items-center justify-center px-4 pb-8">
        <div class="w-full max-w-sm">
          <slot />
        </div>
      </div>
    </div>
  </div>
</template>