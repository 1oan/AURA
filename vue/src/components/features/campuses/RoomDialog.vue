<script setup lang="ts">
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ApiError } from '@/api/client'
import { updateRoom } from '@/api/rooms'
import type { RoomDto } from '@/api/rooms'

interface Props {
  open: boolean
  room: RoomDto | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: [room: RoomDto]
}>()

const form = ref({ number: '', floor: 0, capacity: 2, gender: 'Male' })
const error = ref('')
const saving = ref(false)

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen && props.room) {
      form.value = {
        number: props.room.number,
        floor: props.room.floor,
        capacity: props.room.capacity,
        gender: props.room.gender,
      }
      error.value = ''
    }
  },
)

async function save() {
  error.value = ''
  if (!form.value.number.trim()) {
    error.value = 'Room number is required.'
    return
  }
  saving.value = true
  try {
    await updateRoom(props.room!.id, {
      number: form.value.number.trim(),
      floor: form.value.floor,
      capacity: form.value.capacity,
      gender: form.value.gender,
    })
    toast.success('Room updated successfully.')
    emit('saved', {
      id: props.room!.id,
      dormitoryId: props.room!.dormitoryId,
      number: form.value.number.trim(),
      floor: form.value.floor,
      capacity: form.value.capacity,
      gender: form.value.gender,
    })
    emit('update:open', false)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>Edit Room</DialogTitle>
        <DialogDescription>Update room details.</DialogDescription>
      </DialogHeader>
      <form class="space-y-4" @submit.prevent="save">
        <div class="grid grid-cols-2 gap-4">
          <div class="space-y-2">
            <Label for="room-number">Room Number</Label>
            <Input id="room-number" v-model="form.number" required />
          </div>
          <div class="space-y-2">
            <Label for="room-floor">Floor</Label>
            <Input id="room-floor" v-numeric v-model.number="form.floor" type="text" inputmode="numeric" required />
          </div>
          <div class="space-y-2">
            <Label for="room-capacity">Capacity</Label>
            <Input id="room-capacity" v-numeric v-model.number="form.capacity" type="text" inputmode="numeric" required />
          </div>
          <div class="space-y-2">
            <Label>Gender</Label>
            <Select v-model="form.gender">
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Male">Male</SelectItem>
                <SelectItem value="Female">Female</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </div>
        <p v-if="error" role="alert" class="text-sm text-destructive">{{ error }}</p>
        <DialogFooter>
          <Button type="submit" :disabled="saving">
            {{ saving ? 'Saving...' : 'Save' }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
