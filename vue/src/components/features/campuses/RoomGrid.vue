<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { BedDouble, Pencil, Trash2, Plus, X, Layers } from 'lucide-vue-next'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/api/client'
import { createRoom } from '@/api/rooms'
import type { RoomDto } from '@/api/rooms'

const props = defineProps<{
  rooms: RoomDto[]
  dormitoryId: string
  loading: boolean
}>()

const emit = defineEmits<{
  edit: [room: RoomDto]
  delete: [room: RoomDto]
  bulkCreate: []
  roomCreated: [room: RoomDto]
}>()

// --- Inline Add State ---
const inlineAdd = ref<{ floor: number; gender: string } | null>(null)
const inlineAddNumber = ref('')
const inlineAddCapacity = ref(3)
const inlineAddSaving = ref(false)

function startInlineAdd(floor: number, gender: string, capacity: number) {
  inlineAdd.value = { floor, gender }
  inlineAddNumber.value = ''
  inlineAddCapacity.value = capacity
}

function cancelInlineAdd() {
  inlineAdd.value = null
}

async function submitInlineAdd() {
  if (!inlineAdd.value || !inlineAddNumber.value.trim()) return
  inlineAddSaving.value = true
  try {
    const newRoom = await createRoom({
      number: inlineAddNumber.value.trim(),
      dormitoryId: props.dormitoryId,
      floor: inlineAdd.value.floor,
      capacity: inlineAddCapacity.value,
      gender: inlineAdd.value.gender,
    })
    emit('roomCreated', newRoom)
    toast.success(`Room ${newRoom.number} added.`)
    inlineAdd.value = null
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Failed to create room.')
    }
  } finally {
    inlineAddSaving.value = false
  }
}

// --- Computed ---
const roomsByFloor = computed(() => {
  const grouped = new Map<number, RoomDto[]>()
  for (const room of props.rooms) {
    if (!grouped.has(room.floor)) grouped.set(room.floor, [])
    grouped.get(room.floor)!.push(room)
  }
  const sorted = [...grouped.entries()].sort(([a], [b]) => a - b)
  for (const [, floorRooms] of sorted) {
    floorRooms.sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }))
  }
  return sorted
})

function genderColor(gender: string) {
  return gender === 'Female'
    ? 'border-amber-500/30 bg-amber-500/5'
    : 'border-primary/30 bg-primary/5'
}
</script>

<template>
  <!-- Loading -->
  <div v-if="loading" class="space-y-4">
    <Skeleton class="h-6 w-24" />
    <div class="grid grid-cols-5 gap-1 sm:grid-cols-7 lg:grid-cols-10">
      <Skeleton v-for="i in 20" :key="i" class="h-9" />
    </div>
  </div>

  <!-- Empty -->
  <div
    v-else-if="rooms.length === 0"
    class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
  >
    <BedDouble class="mb-3 size-10 text-muted-foreground" />
    <p class="text-sm font-medium">No rooms</p>
    <p class="mb-4 text-xs text-muted-foreground">Generate rooms for this dormitory.</p>
    <Button size="sm" @click="emit('bulkCreate')">
      <Layers class="mr-1.5 size-3.5" />
      Generate Rooms
    </Button>
  </div>

  <!-- Room grid by floor -->
  <div v-else class="space-y-4">
    <!-- Actions -->
    <div class="flex gap-2">
      <Button variant="outline" size="sm" class="h-7 text-xs" @click="emit('bulkCreate')">
        <Layers class="mr-1.5 size-3" />
        Generate Rooms
      </Button>
    </div>

    <div v-for="[floor, floorRooms] in roomsByFloor" :key="floor" class="space-y-1.5">
      <p class="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
        Floor {{ floor }}
        <span class="normal-case tracking-normal">({{ floorRooms[0]?.gender }}, Cap {{ floorRooms[0]?.capacity }})</span>
      </p>

      <div class="grid grid-cols-5 gap-1 sm:grid-cols-7 lg:grid-cols-10">
        <div
          v-for="room in floorRooms"
          :key="room.id"
          class="group relative flex items-center justify-center rounded border px-1.5 py-1.5 text-xs transition-colors"
          :class="genderColor(room.gender)"
        >
          <span class="font-mono font-medium">{{ room.number }}</span>
          <Badge
            :variant="room.gender === 'Female' ? 'secondary' : 'default'"
            class="absolute -right-1 -top-1 hidden h-3.5 px-1 text-[8px] group-hover:flex"
          >
            {{ room.gender[0] }}
          </Badge>
          <div class="absolute -bottom-1 right-0 hidden items-center gap-px rounded bg-card shadow-sm group-hover:flex md:hidden md:group-hover:flex">
            <button class="rounded p-0.5 hover:bg-accent" @click="emit('edit', room)">
              <Pencil class="size-2.5" />
            </button>
            <button class="rounded p-0.5 hover:bg-destructive/10" @click="emit('delete', room)">
              <Trash2 class="size-2.5" />
            </button>
          </div>
        </div>

        <!-- Inline add form -->
        <div
          v-if="inlineAdd?.floor === floor"
          class="col-span-2 flex items-center gap-1 rounded border border-primary/40 bg-primary/5 px-2 py-1"
        >
          <input
            v-model="inlineAddNumber"
            type="text"
            placeholder="#"
            class="w-10 rounded border bg-background px-1 py-0.5 text-xs focus:outline-none focus:ring-1 focus:ring-primary"
            :disabled="inlineAddSaving"
            @keydown.enter="submitInlineAdd"
            @keydown.escape="cancelInlineAdd"
          />
          <input
            v-numeric
            :value="inlineAddCapacity"
            type="text"
            inputmode="numeric"
            class="w-8 rounded border bg-background px-1 py-0.5 text-center text-xs"
            :disabled="inlineAddSaving"
            @keydown.enter="submitInlineAdd"
            @keydown.escape="cancelInlineAdd"
            @beforeinput="(e: InputEvent) => { if (e.data && !/^\d$/.test(e.data)) e.preventDefault() }"
            @input="(e: Event) => { inlineAddCapacity = parseInt((e.target as HTMLInputElement).value) || 1 }"
          />
          <Button size="sm" class="h-5 px-1.5 text-[10px]" :disabled="inlineAddSaving || !inlineAddNumber.trim()" @click="submitInlineAdd">
            Add
          </Button>
          <button class="rounded p-0.5 text-muted-foreground hover:text-foreground" @click="cancelInlineAdd">
            <X class="size-3" />
          </button>
        </div>

        <!-- Add button -->
        <button
          v-else
          class="flex items-center justify-center rounded border border-dashed py-1.5 text-muted-foreground transition-colors hover:border-primary hover:text-primary"
          @click="startInlineAdd(floor, floorRooms[0]?.gender ?? 'Male', floorRooms[0]?.capacity ?? 3)"
        >
          <Plus class="size-3.5" />
        </button>
      </div>
    </div>
  </div>
</template>
