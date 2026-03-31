<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Card, CardContent } from '@/components/ui/card'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { Building2, DoorOpen, BedDouble, MapPin } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { useCampusDrillDown } from '@/composables/useCampusDrillDown'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { getFaculties } from '@/api/faculties'
import type { FacultyDto } from '@/api/faculties'
import { getCampuses } from '@/api/campuses'
import type { CampusDto } from '@/api/campuses'
import {
  getFacultyRoomAllocations,
  assignRooms,
  removeRoomAssignments,
} from '@/api/facultyRoomAllocations'
import type { FacultyRoomAllocationDto } from '@/api/facultyRoomAllocations'
import type { RoomDto } from '@/api/rooms'

const { level, selectedCampus, selectedDormitory, drilling, breadcrumbs, drillToCampus, drillToDormitory } = useCampusDrillDown()

// --- State ---
const periods = ref<AllocationPeriodDto[]>([])
const faculties = ref<FacultyDto[]>([])
const selectedPeriodId = ref('')
const selectedFacultyId = ref('')
const allAllocations = ref<FacultyRoomAllocationDto[]>([])
const campuses = ref<CampusDto[]>([])
const selectedRoomIds = ref<Set<string>>(new Set())

const loadingInitial = ref(true)
const loadingAllocations = ref(false)
const actionLoading = ref(false)
const actionError = ref('')

// --- Computed ---
const allocationsByRoom = computed(() => {
  const map = new Map<string, FacultyRoomAllocationDto>()
  for (const alloc of allAllocations.value) {
    map.set(alloc.roomId, alloc)
  }
  return map
})

const facultyMap = computed(() => {
  const map = new Map<string, FacultyDto>()
  for (const f of faculties.value) {
    map.set(f.id, f)
  }
  return map
})

const assignedToCurrentCount = computed(() => {
  if (!selectedFacultyId.value) return 0
  return allAllocations.value.filter((a) => a.facultyId === selectedFacultyId.value).length
})

const selectedCurrentFacultyName = computed(() => {
  const f = facultyMap.value.get(selectedFacultyId.value)
  return f?.abbreviation ?? ''
})

// Prepend "Room Quotas" to the composable breadcrumbs
const pageBreadcrumbs = computed(() => {
  const crumbs = breadcrumbs.value.map((c) => ({ ...c }))
  crumbs[0] = {
    label: 'Room Quotas',
    action: crumbs[0]?.action,
  }
  return crumbs
})

function isAssignedToCurrentFaculty(roomId: string): boolean {
  const alloc = allocationsByRoom.value.get(roomId)
  return alloc?.facultyId === selectedFacultyId.value
}

function isAssignedToOtherFaculty(roomId: string): boolean {
  const alloc = allocationsByRoom.value.get(roomId)
  return !!alloc && alloc.facultyId !== selectedFacultyId.value
}

function getFacultyAbbreviationForRoom(roomId: string): string {
  const alloc = allocationsByRoom.value.get(roomId)
  if (!alloc) return ''
  const f = facultyMap.value.get(alloc.facultyId)
  return f?.abbreviation ?? ''
}

function roomsByFloor(rooms: RoomDto[]) {
  const grouped = new Map<number, RoomDto[]>()
  for (const room of rooms) {
    if (!grouped.has(room.floor)) grouped.set(room.floor, [])
    grouped.get(room.floor)!.push(room)
  }
  const sorted = [...grouped.entries()].sort(([a], [b]) => a - b)
  for (const [, floorRooms] of sorted) {
    floorRooms.sort((a, b) => a.number.localeCompare(b.number, undefined, { numeric: true }))
  }
  return sorted
}

const selectionMode = computed<'assign' | 'remove' | null>(() => {
  if (selectedRoomIds.value.size === 0) return null
  const firstId = [...selectedRoomIds.value][0]!
  return isAssignedToCurrentFaculty(firstId) ? 'remove' : 'assign'
})

function canSelect(roomId: string): boolean {
  if (isAssignedToOtherFaculty(roomId)) return false
  if (!selectionMode.value) return true
  if (selectionMode.value === 'assign') return !allocationsByRoom.value.has(roomId)
  return isAssignedToCurrentFaculty(roomId)
}

function toggleRoomSelection(roomId: string) {
  if (!canSelect(roomId)) return
  if (selectedRoomIds.value.has(roomId)) {
    selectedRoomIds.value.delete(roomId)
  } else {
    selectedRoomIds.value.add(roomId)
  }
}

// Clear selection when navigating between levels
watch(level, () => {
  selectedRoomIds.value.clear()
})

// --- Data Fetching ---
onMounted(async () => {
  try {
    const [periodsData, facultiesData, campusesData] = await Promise.all([
      getAllocationPeriods(),
      getFaculties(),
      getCampuses(),
    ])
    periods.value = periodsData
    faculties.value = facultiesData
    campuses.value = campusesData

    if (periodsData.length > 0) {
      selectedPeriodId.value = periodsData[0]!.id
    }
    if (facultiesData.length > 0) {
      selectedFacultyId.value = facultiesData[0]!.id
    }
  } catch (e) {
    console.error('Failed to load initial data:', e)
  } finally {
    loadingInitial.value = false
  }
})

async function fetchAllocations() {
  if (!selectedPeriodId.value) return
  loadingAllocations.value = true
  try {
    allAllocations.value = await getFacultyRoomAllocations(selectedPeriodId.value)
  } catch (e) {
    console.error('Failed to load allocations:', e)
  } finally {
    loadingAllocations.value = false
  }
}

watch([selectedPeriodId, selectedFacultyId], () => {
  selectedRoomIds.value.clear()
  if (selectedPeriodId.value) {
    fetchAllocations()
  }
})

// --- Actions ---
async function handleAssign() {
  if (selectedRoomIds.value.size === 0 || !selectedFacultyId.value || !selectedPeriodId.value) return
  actionError.value = ''
  actionLoading.value = true
  try {
    const count = selectedRoomIds.value.size
    await assignRooms({
      facultyId: selectedFacultyId.value,
      allocationPeriodId: selectedPeriodId.value,
      roomIds: [...selectedRoomIds.value],
    })
    selectedRoomIds.value.clear()
    await fetchAllocations()
    toast.success(`${count} room${count !== 1 ? 's' : ''} assigned to ${selectedCurrentFacultyName.value}.`)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Failed to assign rooms.')
    }
  } finally {
    actionLoading.value = false
  }
}

async function handleRemove() {
  if (selectedRoomIds.value.size === 0 || !selectedFacultyId.value || !selectedPeriodId.value) return
  actionError.value = ''
  actionLoading.value = true
  try {
    const count = selectedRoomIds.value.size
    await removeRoomAssignments({
      facultyId: selectedFacultyId.value,
      allocationPeriodId: selectedPeriodId.value,
      roomIds: [...selectedRoomIds.value],
    })
    selectedRoomIds.value.clear()
    await fetchAllocations()
    toast.success(`${count} room assignment${count !== 1 ? 's' : ''} removed.`)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Failed to remove room assignments.')
    }
  } finally {
    actionLoading.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <!-- Selectors (always visible) -->
      <div class="flex flex-wrap items-end gap-3">
        <div class="w-56 space-y-1">
          <label class="text-sm font-medium">Period</label>
          <Select v-model="selectedPeriodId">
            <SelectTrigger>
              <SelectValue placeholder="Select period" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="p in periods" :key="p.id" :value="p.id">
                {{ p.name }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div class="w-56 space-y-1">
          <label class="text-sm font-medium">Faculty</label>
          <Select v-model="selectedFacultyId">
            <SelectTrigger>
              <SelectValue placeholder="Select faculty" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="f in faculties" :key="f.id" :value="f.id">
                {{ f.name }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div v-if="selectedFacultyId && selectedPeriodId" class="flex items-center gap-2 pb-0.5">
          <Badge variant="outline" class="text-sm">
            {{ assignedToCurrentCount }} rooms assigned to {{ selectedCurrentFacultyName }}
          </Badge>
          <Skeleton v-if="loadingAllocations" class="h-5 w-5" />
        </div>
      </div>

      <!-- Breadcrumb -->
      <Breadcrumb>
        <BreadcrumbList>
          <template v-for="(crumb, i) in pageBreadcrumbs" :key="i">
            <BreadcrumbSeparator v-if="i > 0" />
            <BreadcrumbItem>
              <BreadcrumbLink v-if="crumb.action" class="cursor-pointer" @click="crumb.action">
                {{ crumb.label }}
              </BreadcrumbLink>
              <BreadcrumbPage v-else>{{ crumb.label }}</BreadcrumbPage>
            </BreadcrumbItem>
          </template>
        </BreadcrumbList>
      </Breadcrumb>

      <!-- Loading -->
      <div v-if="loadingInitial" class="space-y-3">
        <Skeleton class="h-10 w-full" />
        <Skeleton class="h-64 w-full" />
      </div>

      <!-- No selection state -->
      <div
        v-else-if="!selectedPeriodId || !selectedFacultyId"
        class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
      >
        <p class="text-muted-foreground">Select a period and faculty to manage room allocations.</p>
      </div>

      <!-- Level 1: Campuses -->
      <template v-else-if="level === 'campuses'">
        <div v-if="drilling" class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <Skeleton v-for="i in 6" :key="i" class="h-20" />
        </div>
        <div
          v-else-if="campuses.length === 0"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
        >
          <Building2 class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No campuses found</p>
          <p class="text-xs text-muted-foreground">Create campuses and dormitories first.</p>
        </div>
        <div v-else class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <Card
            v-for="campus in campuses"
            :key="campus.id"
            class="cursor-pointer transition-colors hover:bg-muted/30"
            @click="drillToCampus(campus.id)"
          >
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <Building2 class="size-4" />
              </div>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-medium leading-tight">{{ campus.name }}</p>
                <p v-if="campus.address" class="flex items-center gap-1 truncate text-xs text-muted-foreground">
                  <MapPin class="size-3 shrink-0" />
                  {{ campus.address }}
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      </template>

      <!-- Level 2: Dormitories -->
      <template v-else-if="level === 'dormitories' && selectedCampus">
        <div v-if="drilling" class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <Skeleton v-for="i in 6" :key="i" class="h-16" />
        </div>
        <div
          v-else-if="selectedCampus.dormitories.length === 0"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
        >
          <DoorOpen class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No dormitories</p>
          <p class="text-xs text-muted-foreground">Add dormitories to this campus first.</p>
        </div>
        <div v-else class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
          <Card
            v-for="dorm in selectedCampus.dormitories"
            :key="dorm.id"
            class="cursor-pointer transition-colors hover:bg-muted/30"
            @click="drillToDormitory(dorm.id)"
          >
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <DoorOpen class="size-4" />
              </div>
              <div class="min-w-0 flex-1">
                <p class="text-sm font-medium leading-tight">{{ dorm.name }}</p>
              </div>
            </CardContent>
          </Card>
        </div>
      </template>

      <!-- Level 3: Room selection grid -->
      <template v-else-if="level === 'rooms' && selectedDormitory">
        <div v-if="drilling" class="space-y-3">
          <Skeleton class="h-8 w-full" />
          <Skeleton class="h-8 w-full" />
        </div>
        <div
          v-else-if="selectedDormitory.rooms.length === 0"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
        >
          <BedDouble class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No rooms</p>
          <p class="text-xs text-muted-foreground">Add rooms to this dormitory first.</p>
        </div>
        <div v-else class="space-y-4">
          <div v-for="[floor, rooms] in roomsByFloor(selectedDormitory.rooms)" :key="floor">
            <p class="mb-1.5 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Floor {{ floor }}
              <span class="normal-case tracking-normal">({{ rooms[0]?.gender }}, Cap {{ rooms[0]?.capacity }})</span>
            </p>
            <div class="flex flex-wrap gap-2">
              <button
                v-for="room in rooms"
                :key="room.id"
                type="button"
                class="flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-sm transition-colors"
                :class="{
                  'bg-primary text-primary-foreground border-primary': isAssignedToCurrentFaculty(room.id),
                  'bg-muted text-muted-foreground opacity-50 cursor-not-allowed': isAssignedToOtherFaculty(room.id),
                  'bg-card hover:border-primary cursor-pointer': !isAssignedToCurrentFaculty(room.id) && !isAssignedToOtherFaculty(room.id) && canSelect(room.id),
                  'bg-card opacity-40 cursor-not-allowed': !isAssignedToCurrentFaculty(room.id) && !isAssignedToOtherFaculty(room.id) && !canSelect(room.id),
                  'opacity-40 cursor-not-allowed': isAssignedToCurrentFaculty(room.id) && !canSelect(room.id),
                  'ring-2 ring-primary ring-offset-1': selectedRoomIds.has(room.id),
                }"
                :disabled="!canSelect(room.id)"
                :title="isAssignedToOtherFaculty(room.id) ? `Assigned to ${getFacultyAbbreviationForRoom(room.id)}` : ''"
                @click="toggleRoomSelection(room.id)"
              >
                <BedDouble class="size-3.5" />
                <span class="font-mono font-medium">{{ room.number }}</span>
                <span
                  v-if="isAssignedToOtherFaculty(room.id)"
                  class="text-[10px] font-semibold"
                >
                  {{ getFacultyAbbreviationForRoom(room.id) }}
                </span>
              </button>
            </div>
          </div>
        </div>
      </template>

      <!-- Sticky action bar -->
      <div v-if="selectedRoomIds.size > 0" class="sticky bottom-3">
        <div class="flex items-center justify-between rounded-lg border bg-card px-4 py-3 shadow-md">
          <span class="text-sm font-medium">
            {{ selectedRoomIds.size }} room{{ selectedRoomIds.size !== 1 ? 's' : '' }} selected
          </span>
          <div class="flex items-center gap-2">
            <p v-if="actionError" class="mr-2 text-sm text-destructive">{{ actionError }}</p>
            <Button
              v-if="selectionMode === 'remove'"
              variant="outline"
              size="sm"
              :disabled="actionLoading"
              @click="handleRemove"
            >
              {{ actionLoading ? 'Processing...' : 'Remove Selected' }}
            </Button>
            <Button
              v-if="selectionMode === 'assign'"
              size="sm"
              :disabled="actionLoading"
              @click="handleAssign"
            >
              {{ actionLoading ? 'Processing...' : 'Assign Selected' }}
            </Button>
          </div>
        </div>
      </div>
    </div>
  </AppLayout>
</template>
