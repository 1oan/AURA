<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ChevronRight, Building2, DoorOpen, BedDouble } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { getFaculties } from '@/api/faculties'
import type { FacultyDto } from '@/api/faculties'
import { getCampuses, getCampusById } from '@/api/campuses'
import type { CampusDto } from '@/api/campuses'
import { getDormitoryById } from '@/api/dormitories'
import type { DormitoryDto } from '@/api/dormitories'
import type { RoomDto } from '@/api/rooms'
import {
  getFacultyRoomAllocations,
  assignRooms,
  removeRoomAssignments,
} from '@/api/facultyRoomAllocations'
import type { FacultyRoomAllocationDto } from '@/api/facultyRoomAllocations'

// --- State ---
const periods = ref<AllocationPeriodDto[]>([])
const faculties = ref<FacultyDto[]>([])
const selectedPeriodId = ref('')
const selectedFacultyId = ref('')
const allAllocations = ref<FacultyRoomAllocationDto[]>([])
const campuses = ref<CampusDto[]>([])
const campusDormitories = ref<Map<string, DormitoryDto[]>>(new Map())
const dormitoryRooms = ref<Map<string, RoomDto[]>>(new Map())
const expandedCampuses = ref<Set<string>>(new Set())
const expandedDormitories = ref<Set<string>>(new Set())
const selectedRoomIds = ref<Set<string>>(new Set())

const loadingInitial = ref(true)
const loadingAllocations = ref(false)
const loadingCampuses = ref<Set<string>>(new Set())
const loadingDormitories = ref<Set<string>>(new Set())
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

function roomsByFloor(dormitoryId: string) {
  const rooms = dormitoryRooms.value.get(dormitoryId) ?? []
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

function dormitoryAssignedCount(dormitoryId: string): string | null {
  const rooms = dormitoryRooms.value.get(dormitoryId)
  if (!rooms || !selectedFacultyId.value) return null
  const assigned = rooms.filter((r) => isAssignedToCurrentFaculty(r.id)).length
  return `${assigned}/${rooms.length}`
}

function toggleRoomSelection(roomId: string) {
  if (isAssignedToOtherFaculty(roomId)) return
  if (selectedRoomIds.value.has(roomId)) {
    selectedRoomIds.value.delete(roomId)
  } else {
    selectedRoomIds.value.add(roomId)
  }
}

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

// --- Actions ---
async function handleAssign() {
  if (selectedRoomIds.value.size === 0 || !selectedFacultyId.value || !selectedPeriodId.value) return
  actionError.value = ''
  actionLoading.value = true
  try {
    await assignRooms({
      facultyId: selectedFacultyId.value,
      allocationPeriodId: selectedPeriodId.value,
      roomIds: [...selectedRoomIds.value],
    })
    selectedRoomIds.value.clear()
    await fetchAllocations()
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      actionError.value = data.detail ?? 'Failed to assign rooms.'
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
    await removeRoomAssignments({
      facultyId: selectedFacultyId.value,
      allocationPeriodId: selectedPeriodId.value,
      roomIds: [...selectedRoomIds.value],
    })
    selectedRoomIds.value.clear()
    await fetchAllocations()
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      actionError.value = data.detail ?? 'Failed to remove room assignments.'
    }
  } finally {
    actionLoading.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-6">
      <!-- Header -->
      <div>
        <h1 class="text-3xl font-bold tracking-tight">Room Quotas</h1>
        <p class="mt-1 text-muted-foreground">
          Distribute dormitory rooms to faculties.
        </p>
      </div>

      <!-- Loading -->
      <div v-if="loadingInitial" class="space-y-3">
        <Skeleton class="h-10 w-full" />
        <Skeleton class="h-64 w-full" />
      </div>

      <template v-else>
        <!-- Selectors -->
        <div class="flex flex-wrap items-end gap-4">
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

        <!-- No selection -->
        <div
          v-if="!selectedPeriodId || !selectedFacultyId"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
        >
          <p class="text-muted-foreground">Select a period and faculty to manage room allocations.</p>
        </div>

        <!-- No campuses -->
        <div
          v-else-if="campuses.length === 0"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
        >
          <Building2 class="mb-4 size-12 text-muted-foreground" />
          <p class="text-lg font-medium">No campuses found</p>
          <p class="text-sm text-muted-foreground">Create campuses and dormitories first.</p>
        </div>

        <!-- Campus tree -->
        <template v-else>
          <div class="space-y-3">
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
                <span class="cursor-pointer font-semibold" @click="toggleCampus(campus.id)">
                  {{ campus.name }}
                </span>
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
                    No dormitories in this campus.
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
                            v-if="dormitoryAssignedCount(dorm.id)"
                            variant="outline"
                            class="ml-2"
                          >
                            {{ dormitoryAssignedCount(dorm.id) }} rooms assigned
                          </Badge>
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
                            No rooms in this dormitory.
                          </p>

                          <!-- Rooms grouped by floor -->
                          <div v-else class="space-y-3">
                            <div v-for="[floor, rooms] in roomsByFloor(dorm.id)" :key="floor">
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
                                    'bg-card hover:border-primary cursor-pointer': !isAssignedToCurrentFaculty(room.id) && !isAssignedToOtherFaculty(room.id),
                                    'ring-2 ring-primary ring-offset-1': selectedRoomIds.has(room.id),
                                  }"
                                  :disabled="isAssignedToOtherFaculty(room.id)"
                                  :title="isAssignedToOtherFaculty(room.id) ? `Assigned to ${getFacultyAbbreviationForRoom(room.id)}` : ''"
                                  @click="toggleRoomSelection(room.id)"
                                >
                                  <BedDouble class="size-3.5" />
                                  <span class="font-medium">{{ room.number }}</span>
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
                        </div>
                      </CollapsibleContent>
                    </Collapsible>
                  </div>
                </div>
              </CollapsibleContent>
            </Collapsible>
          </div>

          <!-- Actions -->
          <div v-if="selectedRoomIds.size > 0 || actionError" class="sticky bottom-4">
            <div class="flex items-center justify-between rounded-lg border bg-card px-4 py-3 shadow-md">
              <span class="text-sm font-medium">
                {{ selectedRoomIds.size }} room{{ selectedRoomIds.size !== 1 ? 's' : '' }} selected
              </span>
              <div class="flex items-center gap-2">
                <p v-if="actionError" class="mr-2 text-sm text-destructive">{{ actionError }}</p>
                <Button
                  variant="outline"
                  size="sm"
                  :disabled="actionLoading"
                  @click="handleRemove"
                >
                  {{ actionLoading ? 'Processing...' : 'Remove Selected' }}
                </Button>
                <Button
                  size="sm"
                  :disabled="actionLoading"
                  @click="handleAssign"
                >
                  {{ actionLoading ? 'Processing...' : 'Assign Selected' }}
                </Button>
              </div>
            </div>
          </div>
        </template>
      </template>
    </div>
  </AppLayout>
</template>
