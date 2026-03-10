<script setup lang="ts">
import { Moon, Sun, Monitor } from 'lucide-vue-next'
import { useTheme } from '@/composables/useTheme'

const { theme } = useTheme()

const options = [
  { value: 'light' as const, icon: Sun, label: 'Light' },
  { value: 'dark' as const, icon: Moon, label: 'Dark' },
  { value: 'system' as const, icon: Monitor, label: 'System' },
]
</script>

<template>
  <div class="flex w-full items-center justify-between px-1">
    <span class="text-[13px] font-medium">Theme</span>
    <div class="flex items-center gap-0.5 rounded-lg bg-muted p-0.5">
      <button
        v-for="option in options"
        :key="option.value"
        :class="[
          'flex size-7 items-center justify-center rounded-md transition-all duration-200',
          theme === option.value
            ? 'bg-background text-foreground shadow-sm'
            : 'text-muted-foreground hover:text-foreground',
        ]"
        :title="option.label"
        @click="theme = option.value"
      >
        <component :is="option.icon" class="size-3.5" />
      </button>
    </div>
  </div>
</template>
