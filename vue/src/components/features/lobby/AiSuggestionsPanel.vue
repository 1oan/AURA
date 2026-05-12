<script setup lang="ts">
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { ChevronDown, ChevronUp, Loader2, Lock, Sparkles } from 'lucide-vue-next'
import { RouterLink } from 'vue-router'
import { ApiError } from '@/api/client'
import { getCompatibleSuggestions, inviteToGroup } from '@/api/groups'
import type { CompatibilityCandidateDto } from '@/api/groups'

interface Props {
  groupId: string
  gated: boolean
}

const props = defineProps<Props>()
const emit = defineEmits<{ invited: [] }>()

const expanded = ref(false)
const suggestions = ref<CompatibilityCandidateDto[]>([])
const loading = ref(false)
const invitingId = ref<string | null>(null)
const fetched = ref(false)

const SIGNALS = [
  { label: 'Lifestyle', weight: '40%' },
  { label: 'Personality', weight: '25%' },
  { label: 'Music', weight: '20%' },
  { label: 'Interests', weight: '15%' },
]

async function toggle() {
  if (!props.gated) return
  expanded.value = !expanded.value
  if (expanded.value && !fetched.value) {
    loading.value = true
    try {
      suggestions.value = await getCompatibleSuggestions(props.groupId)
      fetched.value = true
    } finally {
      loading.value = false
    }
  }
}

async function handleInvite(userId: string) {
  invitingId.value = userId
  try {
    await inviteToGroup(props.groupId, userId)
    toast.success('Invitation sent.')
    suggestions.value = suggestions.value.filter(s => s.userId !== userId)
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
  <Card class="overflow-hidden">
    <CardHeader class="p-4 pb-3">
      <CardTitle class="flex items-center gap-2 font-mono text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">
        <Sparkles class="size-3.5 text-primary" />
        AI matches
      </CardTitle>
    </CardHeader>

    <CardContent class="p-0">
      <!-- Locked / gated state -->
      <div v-if="!gated" class="px-4 pb-4 space-y-3">
        <p class="text-xs text-muted-foreground leading-relaxed">
          AURA ranks students in your dorm by compatibility across four signals.
        </p>

        <!-- Signal chips -->
        <div class="flex flex-wrap gap-1.5">
          <span
            v-for="s in SIGNALS"
            :key="s.label"
            class="inline-flex items-center gap-1 rounded-md border border-border/60 bg-muted/40 px-2 py-0.5"
          >
            <span class="font-mono text-[10px] text-muted-foreground">{{ s.label }}</span>
            <span class="font-mono text-[9px] text-muted-foreground/60">{{ s.weight }}</span>
          </span>
        </div>

        <!-- CTA -->
        <div class="rounded-lg border border-dashed border-border/60 bg-muted/20 px-3 py-3 flex items-center justify-between gap-3">
          <div class="flex items-center gap-1.5 text-[10px] font-mono uppercase tracking-wider text-muted-foreground">
            <Lock class="size-3 shrink-0" />
            Profile incomplete
          </div>
          <Button as-child size="sm" class="h-7 shrink-0 text-xs transition-transform active:scale-[0.98]">
            <RouterLink to="/profile">Complete profile</RouterLink>
          </Button>
        </div>
      </div>

      <!-- Unlocked state -->
      <div v-else class="px-4 pb-4 space-y-3">
        <Button
          type="button"
          variant="outline"
          size="sm"
          class="w-full justify-between h-8 text-xs"
          @click="toggle"
        >
          <span>{{ expanded ? 'Hide' : 'Show' }} AI matches</span>
          <component :is="expanded ? ChevronUp : ChevronDown" class="size-3.5" />
        </Button>

        <div v-if="expanded" class="space-y-2">
          <div v-if="loading" class="flex items-center gap-2 text-xs text-muted-foreground">
            <Loader2 class="size-3 animate-spin" />
            Loading suggestions…
          </div>

          <p v-else-if="suggestions.length === 0" class="text-xs text-muted-foreground">
            No compatible matches found in your dorm yet.
          </p>

          <ul v-else class="rounded-md border divide-y">
            <li v-for="s in suggestions" :key="s.userId" class="flex items-center gap-3 px-3 py-2">
              <div class="flex-1 min-w-0">
                <p class="text-sm">{{ s.firstName }} {{ s.lastName }}</p>
                <p class="font-mono text-[10px] text-muted-foreground">
                  {{ Math.round(s.score * 100) }}% match · {{ s.signalsUsed.join(', ') }}
                </p>
              </div>
              <Button
                size="sm"
                variant="outline"
                class="h-7 text-xs transition-transform active:scale-[0.98]"
                :disabled="invitingId === s.userId"
                @click="handleInvite(s.userId)"
              >
                <Loader2 v-if="invitingId === s.userId" class="mr-1 size-3 animate-spin" />
                Invite
              </Button>
            </li>
          </ul>
        </div>
      </div>
    </CardContent>
  </Card>
</template>
