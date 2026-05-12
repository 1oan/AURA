<script setup lang="ts">
import { PhCaretDown as CaretDown } from '@phosphor-icons/vue'
import type { Component } from 'vue'

type Accent = 'slate' | 'violet' | 'accent' | 'emerald'

interface Props {
  icon: Component
  title: string
  status: string
  accent: Accent
  done: boolean
  open: boolean
}

defineProps<Props>()

// Tinted hover shadows via Tailwind's color-aware shadow utility.
const accentMap: Record<Accent, { chip: string; dot: string; shadow: string }> = {
  slate:   { chip: 'bg-slate-500/10 text-slate-700 dark:text-slate-300',       dot: 'bg-slate-500',   shadow: 'group-hover:shadow-md group-hover:shadow-slate-500/10' },
  violet:  { chip: 'bg-violet-500/10 text-violet-700 dark:text-violet-300',    dot: 'bg-violet-500',  shadow: 'group-hover:shadow-md group-hover:shadow-violet-500/10' },
  accent:  { chip: 'bg-accent/15 text-amber-700 dark:text-amber-300',          dot: 'bg-accent',      shadow: 'group-hover:shadow-md group-hover:shadow-amber-500/10' },
  emerald: { chip: 'bg-emerald-500/10 text-emerald-700 dark:text-emerald-300', dot: 'bg-emerald-500', shadow: 'group-hover:shadow-md group-hover:shadow-emerald-500/10' },
}
</script>

<template>
  <span
    :class="[
      'group flex w-full items-center gap-4 px-4 py-3.5 text-left',
      'transition-[background-color,box-shadow] duration-200',
      'hover:bg-muted/30',
      accentMap[accent].shadow,
    ]"
  >
    <span
      :class="[
        'flex size-10 shrink-0 items-center justify-center rounded-lg transition-transform duration-200 group-hover:scale-105',
        accentMap[accent].chip,
      ]"
    >
      <component :is="icon" :size="20" weight="duotone" />
    </span>

    <span class="flex-1 min-w-0">
      <span class="block text-[15px] font-medium leading-tight text-foreground">{{ title }}</span>
      <span class="mt-0.5 flex items-center gap-1.5 text-[13px] text-muted-foreground">
        <span
          v-if="done"
          :class="['size-1.5 shrink-0 rounded-full', accentMap[accent].dot]"
          aria-hidden="true"
        />
        {{ status }}
      </span>
    </span>

    <CaretDown
      :size="16"
      :class="[
        'shrink-0 text-muted-foreground/60 transition-transform duration-200 group-hover:text-muted-foreground',
        open && 'rotate-180',
      ]"
    />
  </span>
</template>
