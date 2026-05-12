<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Crown, Loader2, Lock, LogOut } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { leaveGroup, lockGroup, changeRoomSizePreference } from '@/api/groups'
import type { GroupDto } from '@/api/groups'
import RoomSizeSelector from './RoomSizeSelector.vue'

interface Props {
  group: GroupDto
  currentUserId: string
  mode: 'leader' | 'member' | 'locked'
}

const props = defineProps<Props>()
const emit = defineEmits<{ lock: []; disband: []; leave: []; 'change-preference': [] }>()

const locking = ref(false)
const leaving = ref(false)
const confirmingLeave = ref(false)

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

const expiresIn = computed(() => {
  const ms = new Date(props.group.expiresAt).getTime() - Date.now()
  if (ms <= 0) return 'expired'
  const hours = Math.floor(ms / (1000 * 60 * 60))
  const minutes = Math.floor((ms / (1000 * 60)) % 60)
  return `${hours}h ${minutes}m`
})

const canLock = computed(() =>
  props.group.members.length === props.group.roomSizePreference)

async function handleLock() {
  locking.value = true
  try {
    await lockGroup(props.group.id)
    toast.success('Group locked.')
    emit('lock')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not lock group.')
    }
  } finally {
    locking.value = false
  }
}

async function handleLeave() {
  leaving.value = true
  try {
    await leaveGroup(props.group.id)
    toast.success('You left the group.')
    emit('leave')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not leave group.')
    }
  } finally {
    leaving.value = false
    confirmingLeave.value = false
  }
}

async function handlePrefChange(newPref: 2 | 3) {
  try {
    await changeRoomSizePreference(props.group.id, newPref)
    toast.success('Room size updated.')
    emit('change-preference')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not change preference.')
    }
  }
}
</script>

<template>
  <div
    :class="[
      'rounded-xl border p-5 space-y-4 transition-colors duration-300',
      mode === 'locked'
        ? 'border-emerald-500/30 bg-emerald-500/5'
        : 'border-amber-500/30 bg-amber-500/5',
    ]"
  >
    <!-- Header -->
    <div class="flex items-start justify-between gap-3">
      <div>
        <p class="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">Your group</p>
        <p class="mt-0.5 font-mono text-xl font-semibold tracking-tight">{{ group.dormitoryName }}</p>
        <p class="mt-0.5 text-xs text-muted-foreground">{{ group.roomSizePreference }}-bed room</p>
      </div>
      <div class="flex items-center gap-2 pt-0.5">
        <span v-if="mode !== 'locked'" class="relative flex size-2 shrink-0" aria-hidden="true">
          <span class="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
          <span class="relative inline-flex size-2 rounded-full bg-amber-500" />
        </span>
        <Badge
          :variant="mode === 'locked' ? 'default' : 'outline'"
          class="h-5 shrink-0 font-mono text-[10px]"
        >
          {{ group.status }}
        </Badge>
      </div>
    </div>

    <!-- Member count subheading -->
    <p class="text-xs text-muted-foreground">
      {{ group.members.length }} of {{ group.roomSizePreference }} members
    </p>

    <!-- Member list with staggered entrance -->
    <TransitionGroup tag="ul" name="member" class="space-y-1.5">
      <li
        v-for="(m, i) in group.members"
        :key="m.userId"
        :style="{ '--i': i }"
        class="flex items-center gap-3 rounded-lg border border-border/50 bg-background/60 px-3 py-2"
      >
        <span
          class="flex size-7 shrink-0 items-center justify-center rounded-full font-mono text-[10px] font-semibold"
          :style="getAvatarStyle(m.userId)"
        >
          {{ getInitials(m.firstName, m.lastName) }}
        </span>
        <span class="min-w-0 flex-1 text-sm">{{ m.firstName }} {{ m.lastName }}</span>
        <Crown v-if="m.isLeader" class="size-3 shrink-0 text-amber-500" aria-hidden="true" title="Leader" />
        <span v-if="m.isLeader" class="sr-only">(leader)</span>
        <span v-if="m.userId === currentUserId" class="shrink-0 text-[10px] text-muted-foreground">(you)</span>
      </li>
    </TransitionGroup>

    <!-- Expiry -->
    <p v-if="mode !== 'locked'" class="text-xs text-muted-foreground">
      Expires in <span class="font-mono tabular-nums">{{ expiresIn }}</span>
    </p>

    <!-- Lock hint when not enough members -->
    <p v-if="mode === 'leader' && !canLock" class="text-xs text-muted-foreground">
      Invite {{ group.roomSizePreference - group.members.length }} more to be able to lock.
    </p>

    <!-- Leader actions -->
    <div v-if="mode === 'leader'" class="flex flex-wrap items-center gap-2 border-t border-border/50 pt-3">
      <RoomSizeSelector
        :model-value="(group.roomSizePreference as 2 | 3)"
        :min-members="group.members.length"
        @update:model-value="handlePrefChange"
      />
      <div class="flex-1" />
      <Button
        size="sm"
        class="h-8 text-xs transition-transform active:scale-[0.98]"
        :disabled="!canLock || locking"
        @click="handleLock"
      >
        <Lock v-if="!locking" class="mr-1 size-3.5" />
        <Loader2 v-else class="mr-1 size-3.5 animate-spin" />
        Get rooms for group
      </Button>
      <Button variant="outline" size="sm" class="h-8 text-xs" @click="emit('disband')">
        Disband
      </Button>
    </div>

    <!-- Member: inline two-step leave -->
    <div v-else-if="mode === 'member'" class="border-t border-border/50 pt-3">
      <template v-if="!confirmingLeave">
        <Button variant="outline" size="sm" class="h-8 text-xs" @click="confirmingLeave = true">
          <LogOut class="mr-1 size-3.5" />
          Leave group
        </Button>
      </template>
      <template v-else>
        <div class="flex items-center gap-2">
          <span class="text-xs text-muted-foreground">Leave this group?</span>
          <Button
            size="sm"
            variant="destructive"
            class="h-7 text-xs transition-transform active:scale-[0.98]"
            :disabled="leaving"
            @click="handleLeave"
          >
            <Loader2 v-if="leaving" class="mr-1 size-3 animate-spin" />
            Confirm
          </Button>
          <Button
            size="sm"
            variant="outline"
            class="h-7 text-xs"
            :disabled="leaving"
            @click="confirmingLeave = false"
          >
            Cancel
          </Button>
        </div>
      </template>
    </div>

    <!-- Locked: room assignment message -->
    <p v-else class="border-t border-border/50 pt-3 text-xs text-muted-foreground">
      Room assignment will follow shortly.
    </p>
  </div>
</template>

<style scoped>
.member-enter-active {
  transition: opacity 280ms ease, transform 280ms cubic-bezier(0.16, 1, 0.3, 1);
  transition-delay: calc(var(--i, 0) * 50ms);
}
.member-leave-active {
  transition: opacity 150ms ease;
}
.member-enter-from {
  opacity: 0;
  transform: translateX(-10px);
}
.member-leave-to {
  opacity: 0;
}
</style>
