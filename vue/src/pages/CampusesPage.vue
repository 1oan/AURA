<script setup lang="ts">
import { ref, onMounted } from 'vue'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Plus,
  Pencil,
  Trash2,
  ChevronRight,
  Building2,
  DoorOpen,
  BedDouble,
  Layers,
} from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import {
  getCampuses,
  getCampusById,
  createCampus,
  updateCampus,
  deleteCampus,
} from '@/api/campuses'
import type { CampusDto } from '@/api/campuses'
import {
  createDormitory,
  updateDormitory,
  deleteDormitory,
  getDormitoryById,
} from '@/api/dormitories'
import type { DormitoryDto } from '@/api/dormitories'
import {
  bulkCreateRooms,
  updateRoom,
  deleteRoom,
} from '@/api/rooms'
import type { RoomDto, FloorConfiguration } from '@/api/rooms'

// --- State ---
const campuses = ref<CampusDto[]>([])
const expandedCampuses = ref<Set<string>>(new Set())
const campusDormitories = ref<Map<string, DormitoryDto[]>>(new Map())
const expandedDormitories = ref<Set<string>>(new Set())
const dormitoryRooms = ref<Map<string, RoomDto[]>>(new Map())
const loading = ref(true)
const loadingCampuses = ref<Set<string>>(new Set())
const loadingDormitories = ref<Set<string>>(new Set())

// --- Campus Dialog ---
const campusDialogOpen = ref(false)
const campusDialogMode = ref<'create' | 'edit'>('create')
const campusForm = ref({ id: '', name: '', address: '' })
const campusFormError = ref('')
const campusFormSaving = ref(false)

// --- Dormitory Dialog ---
const dormitoryDialogOpen = ref(false)
const dormitoryDialogMode = ref<'create' | 'edit'>('create')
const dormitoryForm = ref({ id: '', name: '', campusId: '' })
const dormitoryFormError = ref('')
const dormitoryFormSaving = ref(false)

// --- Room Dialog (single) ---
const roomDialogOpen = ref(false)
const roomDialogMode = ref<'create' | 'edit'>('create')
const roomForm = ref({ id: '', number: '', dormitoryId: '', floor: 0, capacity: 2, gender: 'Male' })
const roomFormError = ref('')
const roomFormSaving = ref(false)

// --- Bulk Room Dialog ---
const bulkRoomDialogOpen = ref(false)
const bulkRoomDormitoryId = ref('')
const bulkRoomDormitoryName = ref('')
const floorConfigs = ref<FloorConfiguration[]>([
  { floorNumber: 0, roomCount: 10, capacity: 3, gender: 'Male' },
])
const bulkRoomError = ref('')
const bulkRoomSaving = ref(false)

// --- Delete Dialog ---
const deleteDialogOpen = ref(false)
const deleteTarget = ref<{ type: 'campus' | 'dormitory' | 'room'; id: string; name: string }>({
  type: 'campus',
  id: '',
  name: '',
})
const deleteParentId = ref('')
const deleteError = ref('')
const deleteLoading = ref(false)

// --- Computed helpers ---
function roomsByFloor(dormitoryId: string) {
  const rooms = dormitoryRooms.value.get(dormitoryId) ?? []
  const grouped = new Map<number, RoomDto[]>()
  for (const room of rooms) {
    if (!grouped.has(room.floor)) grouped.set(room.floor, [])
    grouped.get(room.floor)!.push(room)
  }
  // Sort floors and rooms within each floor
  const sorted = [...grouped.entries()].sort(([a], [b]) => a - b)
  for (const [, floorRooms] of sorted) {
    floorRooms.sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }))
  }
  return sorted
}

function dormitoryRoomCount(dormitoryId: string): number | null {
  const rooms = dormitoryRooms.value.get(dormitoryId)
  return rooms ? rooms.length : null
}

// --- Data Fetching ---
onMounted(async () => {
  try {
    campuses.value = await getCampuses()
  } catch (e) {
    console.error('Failed to load campuses:', e)
  } finally {
    loading.value = false
  }
})

async function toggleCampus(campusId: string) {
  if (expandedCampuses.value.has(campusId)) {
    expandedCampuses.value.delete(campusId)
    return
  }
  expandedCampuses.value.add(campusId)
  if (!campusDormitories.value.has(campusId)) {
    loadingCampuses.value.add(campusId)
    try {
      const detail = await getCampusById(campusId)
      campusDormitories.value.set(campusId, detail.dormitories)
    } catch (e) {
      console.error('Failed to load dormitories:', e)
    } finally {
      loadingCampuses.value.delete(campusId)
    }
  }
}

async function toggleDormitory(dormitoryId: string) {
  if (expandedDormitories.value.has(dormitoryId)) {
    expandedDormitories.value.delete(dormitoryId)
    return
  }
  expandedDormitories.value.add(dormitoryId)
  if (!dormitoryRooms.value.has(dormitoryId)) {
    loadingDormitories.value.add(dormitoryId)
    try {
      const detail = await getDormitoryById(dormitoryId)
      dormitoryRooms.value.set(dormitoryId, detail.rooms)
    } catch (e) {
      console.error('Failed to load rooms:', e)
    } finally {
      loadingDormitories.value.delete(dormitoryId)
    }
  }
}

// --- Campus CRUD ---
function openCreateCampus() {
  campusDialogMode.value = 'create'
  campusForm.value = { id: '', name: '', address: '' }
  campusFormError.value = ''
  campusDialogOpen.value = true
}

function openEditCampus(campus: CampusDto) {
  campusDialogMode.value = 'edit'
  campusForm.value = { id: campus.id, name: campus.name, address: campus.address ?? '' }
  campusFormError.value = ''
  campusDialogOpen.value = true
}

async function saveCampus() {
  campusFormError.value = ''
  if (!campusForm.value.name.trim()) {
    campusFormError.value = 'Name is required.'
    return
  }
  campusFormSaving.value = true
  try {
    const payload = {
      name: campusForm.value.name.trim(),
      address: campusForm.value.address.trim() || undefined,
    }
    if (campusDialogMode.value === 'create') {
      const created = await createCampus(payload)
      campuses.value.push(created)
    } else {
      await updateCampus(campusForm.value.id, payload)
      const idx = campuses.value.findIndex((c) => c.id === campusForm.value.id)
      if (idx !== -1) {
        campuses.value[idx] = { id: campuses.value[idx]!.id, name: payload.name, address: payload.address ?? null }
      }
    }
    campusDialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      campusFormError.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    campusFormSaving.value = false
  }
}

function confirmDeleteCampus(campus: CampusDto) {
  deleteTarget.value = { type: 'campus', id: campus.id, name: campus.name }
  deleteParentId.value = ''
  deleteError.value = ''
  deleteDialogOpen.value = true
}

// --- Dormitory CRUD ---
function openCreateDormitory(campusId: string) {
  dormitoryDialogMode.value = 'create'
  dormitoryForm.value = { id: '', name: '', campusId }
  dormitoryFormError.value = ''
  dormitoryDialogOpen.value = true
}

function openEditDormitory(dormitory: DormitoryDto) {
  dormitoryDialogMode.value = 'edit'
  dormitoryForm.value = { id: dormitory.id, name: dormitory.name, campusId: dormitory.campusId }
  dormitoryFormError.value = ''
  dormitoryDialogOpen.value = true
}

async function saveDormitory() {
  dormitoryFormError.value = ''
  if (!dormitoryForm.value.name.trim()) {
    dormitoryFormError.value = 'Name is required.'
    return
  }
  dormitoryFormSaving.value = true
  try {
    if (dormitoryDialogMode.value === 'create') {
      const created = await createDormitory({
        name: dormitoryForm.value.name.trim(),
        campusId: dormitoryForm.value.campusId,
      })
      const existing = campusDormitories.value.get(dormitoryForm.value.campusId) ?? []
      campusDormitories.value.set(dormitoryForm.value.campusId, [...existing, created])
    } else {
      await updateDormitory(dormitoryForm.value.id, { name: dormitoryForm.value.name.trim() })
      const dorms = campusDormitories.value.get(dormitoryForm.value.campusId)
      if (dorms) {
        const idx = dorms.findIndex((d) => d.id === dormitoryForm.value.id)
        if (idx !== -1) dorms[idx] = { id: dorms[idx]!.id, name: dormitoryForm.value.name.trim(), campusId: dorms[idx]!.campusId }
      }
    }
    dormitoryDialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      dormitoryFormError.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    dormitoryFormSaving.value = false
  }
}

function confirmDeleteDormitory(dormitory: DormitoryDto) {
  deleteTarget.value = { type: 'dormitory', id: dormitory.id, name: dormitory.name }
  deleteParentId.value = dormitory.campusId
  deleteError.value = ''
  deleteDialogOpen.value = true
}

// --- Room CRUD ---
function openEditRoom(room: RoomDto) {
  roomDialogMode.value = 'edit'
  roomForm.value = {
    id: room.id,
    number: room.number,
    dormitoryId: room.dormitoryId,
    floor: room.floor,
    capacity: room.capacity,
    gender: room.gender,
  }
  roomFormError.value = ''
  roomDialogOpen.value = true
}

async function saveRoom() {
  roomFormError.value = ''
  if (!roomForm.value.number.trim()) {
    roomFormError.value = 'Room number is required.'
    return
  }
  roomFormSaving.value = true
  try {
    await updateRoom(roomForm.value.id, {
      number: roomForm.value.number.trim(),
      floor: roomForm.value.floor,
      capacity: roomForm.value.capacity,
      gender: roomForm.value.gender,
    })
    const rooms = dormitoryRooms.value.get(roomForm.value.dormitoryId)
    if (rooms) {
      const idx = rooms.findIndex((r) => r.id === roomForm.value.id)
      if (idx !== -1) {
        rooms[idx] = {
          id: rooms[idx]!.id,
          dormitoryId: rooms[idx]!.dormitoryId,
          number: roomForm.value.number.trim(),
          floor: roomForm.value.floor,
          capacity: roomForm.value.capacity,
          gender: roomForm.value.gender,
        }
      }
    }
    roomDialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      roomFormError.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    roomFormSaving.value = false
  }
}

function confirmDeleteRoom(room: RoomDto) {
  deleteTarget.value = { type: 'room', id: room.id, name: `Room ${room.number}` }
  deleteParentId.value = room.dormitoryId
  deleteError.value = ''
  deleteDialogOpen.value = true
}

// --- Bulk Room Creation ---
function openBulkRoomDialog(dormitoryId: string, dormitoryName: string) {
  bulkRoomDormitoryId.value = dormitoryId
  bulkRoomDormitoryName.value = dormitoryName
  floorConfigs.value = [{ floorNumber: 0, roomCount: 10, capacity: 3, gender: 'Male' }]
  bulkRoomError.value = ''
  bulkRoomDialogOpen.value = true
}

function addFloorConfig() {
  const lastFloor = floorConfigs.value[floorConfigs.value.length - 1]
  floorConfigs.value.push({
    floorNumber: (lastFloor?.floorNumber ?? -1) + 1,
    roomCount: lastFloor?.roomCount ?? 10,
    capacity: lastFloor?.capacity ?? 3,
    gender: lastFloor?.gender ?? 'Male',
  })
}

function removeFloorConfig(index: number) {
  floorConfigs.value.splice(index, 1)
}

async function saveBulkRooms() {
  bulkRoomError.value = ''
  if (floorConfigs.value.length === 0) {
    bulkRoomError.value = 'Add at least one floor configuration.'
    return
  }
  for (const config of floorConfigs.value) {
    if (config.roomCount < 1 || config.capacity < 1) {
      bulkRoomError.value = 'Room count and capacity must be at least 1.'
      return
    }
  }
  bulkRoomSaving.value = true
  try {
    await bulkCreateRooms({
      dormitoryId: bulkRoomDormitoryId.value,
      floors: floorConfigs.value,
    })
    // Refresh rooms for this dormitory
    const detail = await getDormitoryById(bulkRoomDormitoryId.value)
    dormitoryRooms.value.set(bulkRoomDormitoryId.value, detail.rooms)
    bulkRoomDialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      bulkRoomError.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    bulkRoomSaving.value = false
  }
}

// --- Delete Handler ---
async function executeDelete() {
  deleteError.value = ''
  deleteLoading.value = true
  try {
    const { type, id } = deleteTarget.value
    if (type === 'campus') {
      await deleteCampus(id)
      campuses.value = campuses.value.filter((c) => c.id !== id)
      expandedCampuses.value.delete(id)
      campusDormitories.value.delete(id)
    } else if (type === 'dormitory') {
      await deleteDormitory(id)
      const dorms = campusDormitories.value.get(deleteParentId.value)
      if (dorms) {
        campusDormitories.value.set(
          deleteParentId.value,
          dorms.filter((d) => d.id !== id),
        )
      }
      expandedDormitories.value.delete(id)
      dormitoryRooms.value.delete(id)
    } else if (type === 'room') {
      await deleteRoom(id)
      const rooms = dormitoryRooms.value.get(deleteParentId.value)
      if (rooms) {
        dormitoryRooms.value.set(
          deleteParentId.value,
          rooms.filter((r) => r.id !== id),
        )
      }
    }
    deleteDialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      deleteError.value = data.detail ?? 'Failed to delete. It may have dependent records.'
    }
  } finally {
    deleteLoading.value = false
  }
}

function genderVariant(gender: string) {
  return gender === 'Female' ? 'secondary' as const : 'default' as const
}
</script>

<template>
  <AppLayout>
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-3xl font-bold tracking-tight">Campuses & Dormitories</h1>
          <p class="mt-1 text-muted-foreground">
            Manage campus structure, dormitories, and rooms.
          </p>
        </div>
        <Button @click="openCreateCampus">
          <Plus class="mr-2 size-4" />
          Add Campus
        </Button>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-3">
        <Skeleton class="h-16 w-full" />
        <Skeleton class="h-16 w-full" />
        <Skeleton class="h-16 w-full" />
      </div>

      <!-- Empty state -->
      <div
        v-else-if="campuses.length === 0"
        class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
      >
        <Building2 class="mb-4 size-12 text-muted-foreground" />
        <p class="text-lg font-medium">No campuses yet</p>
        <p class="mb-4 text-sm text-muted-foreground">Get started by adding your first campus.</p>
        <Button @click="openCreateCampus">
          <Plus class="mr-2 size-4" />
          Add Campus
        </Button>
      </div>

      <!-- Campus Tree -->
      <div v-else class="space-y-3">
        <Collapsible
          v-for="campus in campuses"
          :key="campus.id"
          :open="expandedCampuses.has(campus.id)"
          class="rounded-lg border"
        >
          <div class="flex items-center gap-3 px-4 py-3">
            <CollapsibleTrigger as-child>
              <button
                class="flex items-center gap-2 rounded-md p-1 hover:bg-accent"
                @click="toggleCampus(campus.id)"
              >
                <ChevronRight
                  class="size-4 shrink-0 transition-transform duration-200"
                  :class="{ 'rotate-90': expandedCampuses.has(campus.id) }"
                />
                <Building2 class="size-4 shrink-0 text-muted-foreground" />
              </button>
            </CollapsibleTrigger>
            <div class="min-w-0 flex-1" @click="toggleCampus(campus.id)">
              <span class="cursor-pointer font-semibold">{{ campus.name }}</span>
              <span v-if="campus.address" class="ml-2 text-sm text-muted-foreground">
                {{ campus.address }}
              </span>
            </div>
            <div class="flex shrink-0 items-center gap-1">
              <Button variant="ghost" size="icon" class="size-8" @click="openCreateDormitory(campus.id)">
                <Plus class="size-4" />
              </Button>
              <Button variant="ghost" size="icon" class="size-8" @click="openEditCampus(campus)">
                <Pencil class="size-4" />
              </Button>
              <Button variant="ghost" size="icon" class="size-8" @click="confirmDeleteCampus(campus)">
                <Trash2 class="size-4" />
              </Button>
            </div>
          </div>

          <CollapsibleContent>
            <Separator />
            <div class="px-4 py-3 pl-12">
              <!-- Loading dormitories -->
              <div v-if="loadingCampuses.has(campus.id)" class="space-y-2">
                <Skeleton class="h-10 w-full" />
                <Skeleton class="h-10 w-full" />
              </div>

              <!-- Empty dormitories -->
              <p
                v-else-if="(campusDormitories.get(campus.id) ?? []).length === 0"
                class="py-2 text-sm text-muted-foreground"
              >
                No dormitories. Click + to add one.
              </p>

              <!-- Dormitory list -->
              <div v-else class="space-y-2">
                <Collapsible
                  v-for="dorm in campusDormitories.get(campus.id) ?? []"
                  :key="dorm.id"
                  :open="expandedDormitories.has(dorm.id)"
                  class="rounded-md border"
                >
                  <div class="flex items-center gap-3 px-3 py-2">
                    <CollapsibleTrigger as-child>
                      <button
                        class="flex items-center gap-2 rounded-md p-1 hover:bg-accent"
                        @click="toggleDormitory(dorm.id)"
                      >
                        <ChevronRight
                          class="size-4 shrink-0 transition-transform duration-200"
                          :class="{ 'rotate-90': expandedDormitories.has(dorm.id) }"
                        />
                        <DoorOpen class="size-4 shrink-0 text-muted-foreground" />
                      </button>
                    </CollapsibleTrigger>
                    <div class="min-w-0 flex-1" @click="toggleDormitory(dorm.id)">
                      <span class="cursor-pointer font-medium">{{ dorm.name }}</span>
                      <Badge
                        v-if="dormitoryRoomCount(dorm.id) !== null"
                        variant="outline"
                        class="ml-2"
                      >
                        {{ dormitoryRoomCount(dorm.id) }} rooms
                      </Badge>
                    </div>
                    <div class="flex shrink-0 items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        class="h-7 text-xs"
                        @click="openBulkRoomDialog(dorm.id, dorm.name)"
                      >
                        <Layers class="mr-1 size-3" />
                        Rooms
                      </Button>
                      <Button variant="ghost" size="icon" class="size-7" @click="openEditDormitory(dorm)">
                        <Pencil class="size-3.5" />
                      </Button>
                      <Button variant="ghost" size="icon" class="size-7" @click="confirmDeleteDormitory(dorm)">
                        <Trash2 class="size-3.5" />
                      </Button>
                    </div>
                  </div>

                  <CollapsibleContent>
                    <Separator />
                    <div class="px-3 py-2 pl-10">
                      <!-- Loading rooms -->
                      <div v-if="loadingDormitories.has(dorm.id)" class="space-y-2">
                        <Skeleton class="h-8 w-full" />
                        <Skeleton class="h-8 w-full" />
                      </div>

                      <!-- Empty rooms -->
                      <p
                        v-else-if="(dormitoryRooms.get(dorm.id) ?? []).length === 0"
                        class="py-2 text-sm text-muted-foreground"
                      >
                        No rooms. Use the "Rooms" button to generate them.
                      </p>

                      <!-- Rooms grouped by floor -->
                      <div v-else class="space-y-3">
                        <div v-for="[floor, rooms] in roomsByFloor(dorm.id)" :key="floor">
                          <p class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                            Floor {{ floor }}
                          </p>
                          <div class="flex flex-wrap gap-2">
                            <div
                              v-for="room in rooms"
                              :key="room.id"
                              class="group flex items-center gap-2 rounded-md border px-2.5 py-1.5 text-sm"
                            >
                              <BedDouble class="size-3.5 text-muted-foreground" />
                              <span class="font-medium">{{ room.number }}</span>
                              <Badge :variant="genderVariant(room.gender)" class="text-[10px] px-1.5 py-0">
                                {{ room.gender }}
                              </Badge>
                              <span class="text-xs text-muted-foreground">Cap: {{ room.capacity }}</span>
                              <div class="flex items-center gap-0.5 opacity-0 transition-opacity group-hover:opacity-100">
                                <button
                                  class="rounded p-0.5 hover:bg-accent"
                                  @click="openEditRoom(room)"
                                >
                                  <Pencil class="size-3" />
                                </button>
                                <button
                                  class="rounded p-0.5 hover:bg-destructive/10"
                                  @click="confirmDeleteRoom(room)"
                                >
                                  <Trash2 class="size-3" />
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </CollapsibleContent>
                </Collapsible>
              </div>
            </div>
          </CollapsibleContent>
        </Collapsible>
      </div>
    </div>

    <!-- Campus Dialog -->
    <Dialog v-model:open="campusDialogOpen">
      <DialogContent class="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{{ campusDialogMode === 'create' ? 'Add Campus' : 'Edit Campus' }}</DialogTitle>
          <DialogDescription>
            {{ campusDialogMode === 'create' ? 'Create a new campus.' : 'Update campus details.' }}
          </DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="saveCampus">
          <div class="space-y-2">
            <Label for="campus-name">Name</Label>
            <Input id="campus-name" v-model="campusForm.name" placeholder="e.g. Codrescu" required />
          </div>
          <div class="space-y-2">
            <Label for="campus-address">Address</Label>
            <Input id="campus-address" v-model="campusForm.address" placeholder="e.g. Str. Codrescu Nr. 1" />
          </div>
          <p v-if="campusFormError" class="text-sm text-destructive">{{ campusFormError }}</p>
          <DialogFooter>
            <Button type="submit" :disabled="campusFormSaving">
              {{ campusFormSaving ? 'Saving...' : 'Save' }}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

    <!-- Dormitory Dialog -->
    <Dialog v-model:open="dormitoryDialogOpen">
      <DialogContent class="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{{ dormitoryDialogMode === 'create' ? 'Add Dormitory' : 'Edit Dormitory' }}</DialogTitle>
          <DialogDescription>
            {{ dormitoryDialogMode === 'create' ? 'Create a new dormitory.' : 'Update dormitory details.' }}
          </DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="saveDormitory">
          <div class="space-y-2">
            <Label for="dorm-name">Name</Label>
            <Input id="dorm-name" v-model="dormitoryForm.name" placeholder="e.g. C10" required />
          </div>
          <p v-if="dormitoryFormError" class="text-sm text-destructive">{{ dormitoryFormError }}</p>
          <DialogFooter>
            <Button type="submit" :disabled="dormitoryFormSaving">
              {{ dormitoryFormSaving ? 'Saving...' : 'Save' }}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

    <!-- Edit Room Dialog -->
    <Dialog v-model:open="roomDialogOpen">
      <DialogContent class="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Edit Room</DialogTitle>
          <DialogDescription>Update room details.</DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="saveRoom">
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-2">
              <Label for="room-number">Room Number</Label>
              <Input id="room-number" v-model="roomForm.number" required />
            </div>
            <div class="space-y-2">
              <Label for="room-floor">Floor</Label>
              <Input id="room-floor" v-model.number="roomForm.floor" type="number" min="0" required />
            </div>
            <div class="space-y-2">
              <Label for="room-capacity">Capacity</Label>
              <Input id="room-capacity" v-model.number="roomForm.capacity" type="number" min="1" required />
            </div>
            <div class="space-y-2">
              <Label>Gender</Label>
              <Select v-model="roomForm.gender">
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Male">Male</SelectItem>
                  <SelectItem value="Female">Female</SelectItem>
                  <SelectItem value="Mixed">Mixed</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
          <p v-if="roomFormError" class="text-sm text-destructive">{{ roomFormError }}</p>
          <DialogFooter>
            <Button type="submit" :disabled="roomFormSaving">
              {{ roomFormSaving ? 'Saving...' : 'Save' }}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

    <!-- Bulk Room Dialog -->
    <Dialog v-model:open="bulkRoomDialogOpen">
      <DialogContent class="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Generate Rooms — {{ bulkRoomDormitoryName }}</DialogTitle>
          <DialogDescription>
            Configure rooms per floor. Rooms will be auto-numbered.
          </DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="saveBulkRooms">
          <div class="space-y-3">
            <div
              v-for="(config, index) in floorConfigs"
              :key="index"
              class="grid grid-cols-[1fr_1fr_1fr_1fr_auto] items-end gap-2"
            >
              <div class="space-y-1">
                <Label v-if="index === 0" class="text-xs">Floor</Label>
                <Input v-model.number="config.floorNumber" type="number" min="0" />
              </div>
              <div class="space-y-1">
                <Label v-if="index === 0" class="text-xs">Rooms</Label>
                <Input v-model.number="config.roomCount" type="number" min="1" />
              </div>
              <div class="space-y-1">
                <Label v-if="index === 0" class="text-xs">Capacity</Label>
                <Input v-model.number="config.capacity" type="number" min="1" />
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
                    <SelectItem value="Mixed">Mixed</SelectItem>
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
          <p v-if="bulkRoomError" class="text-sm text-destructive">{{ bulkRoomError }}</p>
          <DialogFooter>
            <Button type="submit" :disabled="bulkRoomSaving">
              {{ bulkRoomSaving ? 'Generating...' : 'Generate Rooms' }}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>

    <!-- Delete Confirmation -->
    <AlertDialog v-model:open="deleteDialogOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Are you sure?</AlertDialogTitle>
          <AlertDialogDescription>
            This will permanently delete <strong>{{ deleteTarget.name }}</strong>.
            <template v-if="deleteTarget.type === 'campus'">
              All dormitories and rooms within it will also be deleted.
            </template>
            <template v-else-if="deleteTarget.type === 'dormitory'">
              All rooms within it will also be deleted.
            </template>
            This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <p v-if="deleteError" class="text-sm text-destructive">{{ deleteError }}</p>
        <AlertDialogFooter>
          <AlertDialogCancel :disabled="deleteLoading">Cancel</AlertDialogCancel>
          <AlertDialogAction
            class="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            :disabled="deleteLoading"
            @click.prevent="executeDelete"
          >
            {{ deleteLoading ? 'Deleting...' : 'Delete' }}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </AppLayout>
</template>
