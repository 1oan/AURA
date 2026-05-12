<script setup lang="ts">
import { ref } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Loader2 } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { createGroup } from '@/api/groups'
import RoomSizeSelector from './RoomSizeSelector.vue'

const emit = defineEmits<{ created: [] }>()

const roomSize = ref<2 | 3>(2)
const creating = ref(false)
const error = ref('')

async function handleCreate() {
  creating.value = true
  error.value = ''
  try {
    await createGroup(roomSize.value)
    toast.success('Group created.')
    emit('created')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not create group.'
    } else {
      error.value = 'Could not create group.'
    }
  } finally {
    creating.value = false
  }
}
</script>

<template>
  <Card class="overflow-hidden">
    <CardContent class="p-0">
      <div class="px-6 pt-6 pb-5">
        <p class="font-mono text-2xl font-bold tracking-tighter leading-tight">Start a group.</p>
        <p class="mt-1.5 max-w-[48ch] text-sm text-muted-foreground">
          Choose a room size, then invite classmates from your dorm to fill the spots.
        </p>
      </div>
      <div class="space-y-4 border-t border-border/60 bg-muted/20 px-6 py-4">
        <div class="space-y-2">
          <p class="font-mono text-[10px] uppercase tracking-widest text-muted-foreground">Room size</p>
          <RoomSizeSelector v-model="roomSize" />
        </div>
        <p v-if="error" class="text-xs text-destructive" role="alert">{{ error }}</p>
        <Button
          type="button"
          class="w-full transition-transform active:scale-[0.98]"
          :disabled="creating"
          @click="handleCreate"
        >
          <Loader2 v-if="creating" class="mr-2 size-4 animate-spin" />
          {{ creating ? 'Creating…' : 'Create group' }}
        </Button>
      </div>
    </CardContent>
  </Card>
</template>
