<script setup lang="ts">
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Loader2 } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { acceptInvitation, declineInvitation } from '@/api/groups'
import type { InvitationDto } from '@/api/groups'

interface Props {
  invitations: InvitationDto[]
}

defineProps<Props>()
const emit = defineEmits<{ accepted: []; declined: [] }>()

const loadingId = ref<string | null>(null)

async function handleAccept(id: string) {
  loadingId.value = id
  try {
    await acceptInvitation(id)
    toast.success('Joined the group.')
    emit('accepted')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not accept invitation.')
    }
  } finally {
    loadingId.value = null
  }
}

async function handleDecline(id: string) {
  loadingId.value = id
  try {
    await declineInvitation(id)
    toast.success('Invitation declined.')
    emit('declined')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not decline invitation.')
    }
  } finally {
    loadingId.value = null
  }
}
</script>

<template>
  <Card>
    <CardHeader class="p-4 pb-2">
      <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
        Invitations ({{ invitations.length }})
      </CardTitle>
    </CardHeader>
    <CardContent class="p-0">
      <ul class="divide-y">
        <li v-for="inv in invitations" :key="inv.id" class="flex items-center gap-3 px-4 py-3">
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium">
              {{ inv.inviterFirstName }} {{ inv.inviterLastName }}
            </p>
            <p class="text-xs text-muted-foreground">
              {{ inv.dormitoryName }} · {{ inv.roomSizePreference }}-bed
            </p>
          </div>
          <Button
            size="sm"
            class="h-7 text-xs transition-transform active:scale-[0.98]"
            :disabled="loadingId === inv.id"
            @click="handleAccept(inv.id)"
          >
            <Loader2 v-if="loadingId === inv.id" class="mr-1 size-3 animate-spin" />
            Accept
          </Button>
          <Button
            variant="outline"
            size="sm"
            class="h-7 text-xs"
            :disabled="loadingId === inv.id"
            @click="handleDecline(inv.id)"
          >
            Decline
          </Button>
        </li>
      </ul>
    </CardContent>
  </Card>
</template>
