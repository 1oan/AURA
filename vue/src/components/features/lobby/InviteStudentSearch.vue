<script setup lang="ts">
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Loader2, Search, Send, Users } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { searchEligibleStudents, inviteToGroup } from '@/api/groups'
import type { EligibleStudentDto } from '@/api/groups'

interface Props {
  groupId: string
  periodId: string
  currentMemberCount: number
  roomSize: number
}

const props = defineProps<Props>()
const emit = defineEmits<{ invited: [] }>()

const query = ref('')
const results = ref<EligibleStudentDto[]>([])
const searching = ref(false)
const invitingId = ref<string | null>(null)
let debounceTimer: ReturnType<typeof setTimeout> | null = null

watch(query, (newQuery) => {
  if (debounceTimer) clearTimeout(debounceTimer)
  if (newQuery.trim().length < 2) {
    results.value = []
    searching.value = false
    return
  }
  debounceTimer = setTimeout(async () => {
    searching.value = true
    try {
      results.value = await searchEligibleStudents(newQuery.trim(), props.periodId)
    } finally {
      searching.value = false
    }
  }, 250)
})

async function handleInvite(userId: string) {
  invitingId.value = userId
  try {
    await inviteToGroup(props.groupId, userId)
    toast.success('Invitation sent.')
    results.value = results.value.filter(r => r.userId !== userId)
    emit('invited')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not send invitation.')
    }
  } finally {
    invitingId.value = null
  }
}
</script>

<template>
  <Card v-if="currentMemberCount < roomSize" class="overflow-hidden">
    <CardHeader class="p-4 pb-3">
      <CardTitle class="font-mono text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">
        Invite a classmate
      </CardTitle>
    </CardHeader>

    <CardContent class="p-0">
      <div class="px-4 pb-1">
        <div class="relative">
          <Search class="absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" aria-hidden="true" />
          <Input
            v-model="query"
            placeholder="Search by name…"
            aria-label="Search eligible classmates by name"
            class="pl-8"
            autocomplete="off"
          />
        </div>
      </div>

      <!-- Idle state -->
      <div v-if="query.trim().length < 2 && !searching" class="px-4 py-4">
        <div class="flex items-start gap-3 rounded-lg border border-dashed border-border/50 px-3 py-3">
          <Users class="mt-0.5 size-4 shrink-0 text-muted-foreground/50" aria-hidden="true" />
          <div class="space-y-0.5">
            <p class="text-xs font-medium text-muted-foreground">Who can you invite?</p>
            <p class="text-xs text-muted-foreground/70 leading-relaxed">
              Students from your dorm in the same period who haven't joined a group yet.
            </p>
          </div>
        </div>
      </div>

      <!-- Searching -->
      <div v-else-if="searching" class="flex items-center gap-2 px-4 py-4 text-xs text-muted-foreground">
        <Loader2 class="size-3 animate-spin" />
        Searching…
      </div>

      <!-- Results -->
      <ul v-else-if="results.length > 0" class="divide-y border-t">
        <li
          v-for="r in results"
          :key="r.userId"
          class="flex items-center gap-3 px-4 py-2.5"
        >
          <div class="flex-1 min-w-0">
            <p class="text-sm">{{ r.firstName }} {{ r.lastName }}</p>
            <p class="font-mono text-[10px] text-muted-foreground">{{ r.matriculationCode }}</p>
          </div>
          <Button
            size="sm"
            variant="outline"
            class="h-7 text-xs transition-transform active:scale-[0.98]"
            :disabled="invitingId === r.userId"
            @click="handleInvite(r.userId)"
          >
            <Send v-if="invitingId !== r.userId" class="mr-1 size-3" aria-hidden="true" />
            <Loader2 v-else class="mr-1 size-3 animate-spin" />
            Invite
          </Button>
        </li>
      </ul>

      <!-- No results -->
      <p v-else-if="query.trim().length >= 2 && !searching" class="px-4 py-4 text-xs text-muted-foreground">
        No eligible students found for <span class="font-medium text-foreground">"{{ query.trim() }}"</span>. Try a different name.
      </p>
    </CardContent>
  </Card>
</template>
