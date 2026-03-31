<script setup lang="ts">
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Plus, Trash2 } from 'lucide-vue-next'
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
import { bulkCreateRooms } from '@/api/rooms'
import type { FloorConfiguration } from '@/api/rooms'

interface Props {
  open: boolean
  dormitoryId: string
  dormitoryName: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  generated: []
}>()

const floorConfigs = ref<FloorConfiguration[]>([])
const error = ref('')
const saving = ref(false)

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      floorConfigs.value = [{ floorNumber: 0, roomCount: 10, capacity: 3, gender: 'Male' }]
      error.value = ''
    }
  },
)

function addFloorConfig() {
  const last = floorConfigs.value[floorConfigs.value.length - 1]
  floorConfigs.value.push({
    floorNumber: (last?.floorNumber ?? -1) + 1,
    roomCount: last?.roomCount ?? 10,
    capacity: last?.capacity ?? 3,
    gender: last?.gender ?? 'Male',
  })
}

function removeFloorConfig(index: number) {
  floorConfigs.value.splice(index, 1)
}

async function save() {
  error.value = ''
  if (floorConfigs.value.length === 0) {
    error.value = 'Add at least one floor configuration.'
    return
  }
  for (const config of floorConfigs.value) {
    if (config.roomCount < 1 || config.capacity < 1) {
      error.value = 'Room count and capacity must be at least 1.'
      return
    }
  }
  saving.value = true
  try {
    await bulkCreateRooms({
      dormitoryId: props.dormitoryId,
      floors: floorConfigs.value,
    })
    toast.success('Rooms generated successfully.')
    emit('generated')
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
    <DialogContent class="sm:max-w-lg">
      <DialogHeader>
        <DialogTitle>Generate Rooms — {{ dormitoryName }}</DialogTitle>
        <DialogDescription>
          Configure rooms per floor. Rooms will be auto-numbered.
        </DialogDescription>
      </DialogHeader>
      <form class="space-y-4" @submit.prevent="save">
        <div class="space-y-3">
          <div
            v-for="(config, index) in floorConfigs"
            :key="index"
            class="grid grid-cols-[1fr_1fr_1fr_1fr_auto] items-end gap-2"
          >
            <div class="space-y-1">
              <Label v-if="index === 0" class="text-xs">Floor</Label>
              <Input v-numeric v-model.number="config.floorNumber" type="text" inputmode="numeric" />
            </div>
            <div class="space-y-1">
              <Label v-if="index === 0" class="text-xs">Rooms</Label>
              <Input v-numeric v-model.number="config.roomCount" type="text" inputmode="numeric" />
            </div>
            <div class="space-y-1">
              <Label v-if="index === 0" class="text-xs">Capacity</Label>
              <Input v-numeric v-model.number="config.capacity" type="text" inputmode="numeric" />
            </div>
            <div class="space-y-1">
              <Label v-if="index === 0" class="text-xs">Gender</Label>
              <Select v-model="config.gender">
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Male">Male</SelectItem>
                  <SelectItem value="Female">Female</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              class="size-8"
              :disabled="floorConfigs.length <= 1"
              @click="removeFloorConfig(index)"
            >
              <Trash2 class="size-4" />
            </Button>
          </div>
        </div>
        <Button type="button" variant="outline" size="sm" @click="addFloorConfig">
          <Plus class="mr-1 size-4" />
          Add Floor
        </Button>
        <p v-if="error" role="alert" class="text-sm text-destructive">{{ error }}</p>
        <DialogFooter>
          <Button type="submit" :disabled="saving">
            {{ saving ? 'Generating...' : 'Generate Rooms' }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
