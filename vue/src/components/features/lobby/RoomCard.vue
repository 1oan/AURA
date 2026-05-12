<script setup lang="ts">
import { CheckCircle2, KeyRound } from 'lucide-vue-next'
import type { RoomAssignmentDto } from '@/api/rooms'

interface Props {
  assignment: RoomAssignmentDto
}

const props = defineProps<Props>()

const AVATAR_PALETTES = [
  { bg: 'oklch(0.93 0.04 267)', fg: 'oklch(0.28 0.12 267)' },
  { bg: 'oklch(0.94 0.08 70)',  fg: 'oklch(0.45 0.14 70)'  },
  { bg: 'oklch(0.91 0.06 160)', fg: 'oklch(0.38 0.13 160)' },
  { bg: 'oklch(0.93 0.03 290)', fg: 'oklch(0.36 0.09 290)' },
  { bg: 'oklch(0.93 0.05 25)',  fg: 'oklch(0.43 0.12 25)'  },
]

function getInitials(firstName: string, lastName: string): string {
  return `${firstName[0] ?? ''}${lastName[0] ?? ''}`.toUpperCase()
}

function getAvatarStyle(userId: string): Record<string, string> {
  const palette = AVATAR_PALETTES[userId.charCodeAt(0) % AVATAR_PALETTES.length]!
  return { backgroundColor: palette.bg, color: palette.fg }
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}
</script>

<template>
  <article
    class="relative overflow-hidden rounded-2xl border border-emerald-500/25 bg-gradient-to-br from-emerald-500/[0.07] via-emerald-500/[0.04] to-transparent p-6 shadow-[0_1px_2px_0_oklch(0.5_0.15_160_/_0.06),0_8px_24px_-12px_oklch(0.5_0.15_160_/_0.18)]"
  >
    <!-- Decorative ambient glow -->
    <div
      class="pointer-events-none absolute -right-20 -top-20 size-64 rounded-full opacity-40"
      style="background: radial-gradient(circle, oklch(0.78 0.18 160 / 0.25) 0%, transparent 70%)"
      aria-hidden="true"
    />

    <!-- Header strip -->
    <header class="relative flex items-start justify-between gap-4">
      <div class="flex items-start gap-3">
        <span
          class="flex size-9 shrink-0 items-center justify-center rounded-xl bg-emerald-500/15 ring-1 ring-emerald-500/30"
          aria-hidden="true"
        >
          <KeyRound class="size-4 text-emerald-600 dark:text-emerald-400" />
        </span>
        <div>
          <p class="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-700/70 dark:text-emerald-400/70">
            Room assigned
          </p>
          <p class="mt-0.5 text-xs text-muted-foreground">
            {{ props.assignment.dormitoryName }} &middot; Floor {{ props.assignment.floor }}
          </p>
        </div>
      </div>
      <CheckCircle2 class="size-5 shrink-0 text-emerald-500/80" aria-hidden="true" />
    </header>

    <!-- Hero: room number -->
    <div class="relative mt-6 flex items-baseline gap-3">
      <span class="font-mono text-6xl font-bold tracking-tighter leading-none tabular-nums">
        {{ props.assignment.roomNumber }}
      </span>
      <span class="text-sm text-muted-foreground">
        {{ props.assignment.capacity }}-bed
      </span>
    </div>

    <!-- Roommates: horizontal avatar stack with names below -->
    <div class="relative mt-6">
      <p class="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">
        {{ props.assignment.roommates.length === 0 ? 'No roommates' : 'Your roommates' }}
      </p>
      <ul
        v-if="props.assignment.roommates.length > 0"
        class="mt-3 flex flex-wrap gap-x-5 gap-y-3"
      >
        <li
          v-for="roommate in props.assignment.roommates"
          :key="roommate.userId"
          class="flex items-center gap-2.5"
        >
          <span
            class="flex size-9 shrink-0 items-center justify-center rounded-xl font-mono text-[11px] font-semibold ring-1 ring-black/[0.03] dark:ring-white/10"
            :style="getAvatarStyle(roommate.userId)"
          >
            {{ getInitials(roommate.firstName, roommate.lastName) }}
          </span>
          <span class="text-sm font-medium">
            {{ roommate.firstName }} {{ roommate.lastName }}
          </span>
        </li>
      </ul>
    </div>

    <!-- Footer: assigned timestamp -->
    <footer class="relative mt-6 flex items-center gap-2 border-t border-emerald-500/15 pt-3 text-[11px] text-muted-foreground">
      <span class="size-1 rounded-full bg-emerald-500/60" aria-hidden="true" />
      Assigned <time class="tabular-nums" :datetime="props.assignment.assignedAt">{{ formatDate(props.assignment.assignedAt) }}</time>
    </footer>
  </article>
</template>
