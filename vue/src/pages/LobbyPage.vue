<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Skeleton } from '@/components/ui/skeleton'
import GroupCard from '@/components/features/lobby/GroupCard.vue'
import CreateGroupCard from '@/components/features/lobby/CreateGroupCard.vue'
import InvitationsList from '@/components/features/lobby/InvitationsList.vue'
import InviteStudentSearch from '@/components/features/lobby/InviteStudentSearch.vue'
import AiSuggestionsPanel from '@/components/features/lobby/AiSuggestionsPanel.vue'
import DisbandGroupDialog from '@/components/features/lobby/DisbandGroupDialog.vue'
import { useAuthStore } from '@/stores/auth'
import { getMyGroup, getMyInvitations } from '@/api/groups'
import type { GroupDto, InvitationDto } from '@/api/groups'
import { getMyProfile } from '@/api/profile'

const auth = useAuthStore()
const myGroup = ref<GroupDto | null>(null)
const myInvitations = ref<InvitationDto[]>([])
const hasLifestyleData = ref(false)
const loading = ref(true)
const disbandDialogOpen = ref(false)

const currentUserId = computed(() => auth.user?.id ?? '')
const isLeader = computed(() => myGroup.value?.leaderUserId === currentUserId.value)

const mode = computed<'no-group' | 'forming-leader' | 'forming-member' | 'locked'>(() => {
  if (!myGroup.value) return 'no-group'
  if (myGroup.value.status === 'Locked') return 'locked'
  return isLeader.value ? 'forming-leader' : 'forming-member'
})

async function refresh() {
  const [g, invs] = await Promise.all([getMyGroup(), getMyInvitations()])
  myGroup.value = g
  myInvitations.value = invs
}

onMounted(async () => {
  try {
    const [, profile] = await Promise.all([
      refresh(),
      getMyProfile().catch(() => null),
    ])
    hasLifestyleData.value = profile?.hasLifestyleData ?? false
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <AppLayout>
    <div class="max-w-4xl space-y-6">
      <!-- Page header -->
      <div>
        <h1 class="font-mono text-3xl font-bold tracking-tighter leading-none">Lobby</h1>
        <p class="mt-2 max-w-[65ch] text-sm text-muted-foreground">
          Form a roommate group with classmates in your dorm, or accept an invitation.
        </p>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading" class="grid grid-cols-1 gap-3 lg:grid-cols-[1fr_360px]">
        <Skeleton class="h-52" />
        <Skeleton class="h-36" />
      </div>

      <!-- Mode content with slide-up transition on switch -->
      <Transition v-else name="lobby-mode" mode="out-in">
        <!-- No group: editorial CTA + optional invitations -->
        <div v-if="mode === 'no-group'" key="no-group" class="space-y-4">
          <div
            :class="[
              'grid grid-cols-1 gap-4 items-start',
              myInvitations.length > 0 ? 'md:grid-cols-[1fr_300px]' : '',
            ]"
          >
            <CreateGroupCard @created="refresh" />
            <InvitationsList
              v-if="myInvitations.length > 0"
              :invitations="myInvitations"
              @accepted="refresh"
              @declined="refresh"
            />
          </div>
        </div>

        <!-- Leader: asymmetric 2-column grid -->
        <div v-else-if="mode === 'forming-leader' && myGroup" key="forming-leader" class="space-y-4">
          <div class="grid grid-cols-1 gap-4 items-start lg:grid-cols-[1fr_360px]">
            <GroupCard
              :group="myGroup"
              :current-user-id="currentUserId"
              mode="leader"
              @lock="refresh"
              @disband="disbandDialogOpen = true"
              @leave="refresh"
              @change-preference="refresh"
            />
            <div class="space-y-4">
              <InviteStudentSearch
                :group-id="myGroup.id"
                :period-id="myGroup.allocationPeriodId"
                :current-member-count="myGroup.members.length"
                :room-size="myGroup.roomSizePreference"
                @invited="refresh"
              />
              <AiSuggestionsPanel
                :group-id="myGroup.id"
                :gated="hasLifestyleData"
                @invited="refresh"
              />
            </div>
          </div>
          <DisbandGroupDialog
            v-model:open="disbandDialogOpen"
            :group-id="myGroup.id"
            @disbanded="async () => { disbandDialogOpen = false; await refresh() }"
          />
        </div>

        <!-- Member: full-width group card -->
        <div v-else-if="mode === 'forming-member' && myGroup" key="forming-member">
          <GroupCard
            :group="myGroup"
            :current-user-id="currentUserId"
            mode="member"
            @leave="refresh"
          />
        </div>

        <!-- Locked: full-width locked state -->
        <div v-else-if="mode === 'locked' && myGroup" key="locked">
          <GroupCard
            :group="myGroup"
            :current-user-id="currentUserId"
            mode="locked"
          />
        </div>
      </Transition>
    </div>
  </AppLayout>
</template>

<style scoped>
.lobby-mode-enter-active {
  transition: opacity 220ms ease, transform 220ms cubic-bezier(0.16, 1, 0.3, 1);
}
.lobby-mode-leave-active {
  transition: opacity 130ms ease;
}
.lobby-mode-enter-from {
  opacity: 0;
  transform: translateY(6px);
}
.lobby-mode-leave-to {
  opacity: 0;
}
</style>
