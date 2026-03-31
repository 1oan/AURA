<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import AppLayout from '@/components/layout/AppLayout.vue'
import CampusList from '@/components/features/campuses/CampusList.vue'
import DormitoryList from '@/components/features/campuses/DormitoryList.vue'
import RoomGrid from '@/components/features/campuses/RoomGrid.vue'
import CampusDialog from '@/components/features/campuses/CampusDialog.vue'
import DormitoryDialog from '@/components/features/campuses/DormitoryDialog.vue'
import RoomDialog from '@/components/features/campuses/RoomDialog.vue'
import BulkRoomDialog from '@/components/features/campuses/BulkRoomDialog.vue'
import DeleteConfirmDialog from '@/components/features/campuses/DeleteConfirmDialog.vue'
import { useCampusDrillDown } from '@/composables/useCampusDrillDown'
import { getCampuses, deleteCampus } from '@/api/campuses'
import { deleteDormitory } from '@/api/dormitories'
import { deleteRoom } from '@/api/rooms'
import { ApiError } from '@/api/client'
import { Button } from '@/components/ui/button'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import { Plus } from 'lucide-vue-next'
import type { CampusDto } from '@/api/campuses'
import type { DormitoryDto } from '@/api/dormitories'
import type { RoomDto } from '@/api/rooms'

const { level, selectedCampus, selectedDormitory, drilling, breadcrumbs, drillToCampus, drillToDormitory, refreshCampus, refreshDormitory } = useCampusDrillDown()

const campuses = ref<CampusDto[]>([])
const loading = ref(false)

// Dialog state
const campusDialogOpen = ref(false)
const campusDialogMode = ref<'create' | 'edit'>('create')
const campusDialogTarget = ref<CampusDto | null>(null)

const dormitoryDialogOpen = ref(false)
const dormitoryDialogMode = ref<'create' | 'edit'>('create')
const dormitoryDialogTarget = ref<DormitoryDto | null>(null)

const roomDialogOpen = ref(false)
const roomDialogTarget = ref<RoomDto | null>(null)

const bulkRoomDialogOpen = ref(false)

// Delete state
const deleteOpen = ref(false)
const deleteName = ref('')
const deleteError = ref('')
const deleteLoading = ref(false)
const deleteAction = ref<(() => Promise<void>) | null>(null)

async function fetchCampuses() {
  loading.value = true
  try { campuses.value = await getCampuses() } finally { loading.value = false }
}

onMounted(fetchCampuses)

// --- Campus handlers ---
function onCampusEdit(campus: CampusDto) {
  campusDialogMode.value = 'edit'
  campusDialogTarget.value = campus ?? null
  campusDialogOpen.value = true
}
function onCampusDelete(campus: CampusDto) {
  deleteName.value = campus.name
  deleteAction.value = async () => {
    await deleteCampus(campus.id)
    toast.success(`Campus "${campus.name}" deleted.`)
    await fetchCampuses()
  }
  deleteOpen.value = true
}
function onCampusCreate() {
  campusDialogMode.value = 'create'
  campusDialogTarget.value = null
  campusDialogOpen.value = true
}
async function onCampusSaved() { await fetchCampuses() }

// --- Dormitory handlers ---
function onDormitoryEdit(dorm: DormitoryDto) {
  dormitoryDialogMode.value = 'edit'
  dormitoryDialogTarget.value = dorm
  dormitoryDialogOpen.value = true
}
function onDormitoryDelete(dorm: DormitoryDto) {
  deleteName.value = dorm.name
  deleteAction.value = async () => {
    await deleteDormitory(dorm.id)
    toast.success(`Dormitory "${dorm.name}" deleted.`)
    await refreshCampus()
  }
  deleteOpen.value = true
}
function onDormitoryCreate() {
  dormitoryDialogMode.value = 'create'
  dormitoryDialogTarget.value = null
  dormitoryDialogOpen.value = true
}
async function onDormitorySaved() { await refreshCampus() }

// --- Room handlers ---
function onRoomEdit(room: RoomDto) {
  roomDialogTarget.value = room
  roomDialogOpen.value = true
}
function onRoomDelete(room: RoomDto) {
  deleteName.value = `Room ${room.number}`
  deleteAction.value = async () => {
    await deleteRoom(room.id)
    toast.success(`Room ${room.number} deleted.`)
    await refreshDormitory()
  }
  deleteOpen.value = true
}

// --- Delete confirm ---
async function onDeleteConfirmed() {
  if (!deleteAction.value) return
  deleteLoading.value = true
  deleteError.value = ''
  try {
    await deleteAction.value()
    deleteOpen.value = false
  } catch (e) {
    deleteError.value = e instanceof ApiError
      ? ((e.data as { detail?: string }).detail ?? 'Delete failed.')
      : 'An unexpected error occurred.'
  } finally {
    deleteLoading.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <div class="flex items-center justify-between">
        <Breadcrumb>
          <BreadcrumbList>
            <template v-for="(crumb, i) in breadcrumbs" :key="i">
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

        <Button v-if="level === 'campuses'" size="sm" @click="onCampusCreate">
          <Plus class="mr-1.5 size-3.5" /> Add Campus
        </Button>
        <Button v-else-if="level === 'dormitories'" size="sm" @click="onDormitoryCreate">
          <Plus class="mr-1.5 size-3.5" /> Add Dormitory
        </Button>
      </div>

      <CampusList
        v-if="level === 'campuses'"
        :campuses="campuses"
        :loading="loading || drilling"
        @select="drillToCampus"
        @edit="onCampusEdit"
        @delete="onCampusDelete"
        @create="onCampusCreate"
      />

      <DormitoryList
        v-else-if="level === 'dormitories' && selectedCampus"
        :dormitories="selectedCampus.dormitories"
        :loading="drilling"
        @select="drillToDormitory"
        @edit="onDormitoryEdit"
        @delete="onDormitoryDelete"
        @create="onDormitoryCreate"
      />

      <RoomGrid
        v-else-if="level === 'rooms' && selectedDormitory"
        :rooms="selectedDormitory.rooms"
        :dormitory-id="selectedDormitory.id"
        :loading="drilling"
        @edit="onRoomEdit"
        @delete="onRoomDelete"
        @bulk-create="bulkRoomDialogOpen = true"
        @room-created="refreshDormitory"
      />
    </div>

    <CampusDialog
      v-model:open="campusDialogOpen"
      :mode="campusDialogMode"
      :campus="campusDialogTarget"
      @saved="onCampusSaved"
    />
    <DormitoryDialog
      v-model:open="dormitoryDialogOpen"
      :mode="dormitoryDialogMode"
      :dormitory="dormitoryDialogTarget"
      :campus-id="selectedCampus?.id ?? ''"
      @saved="onDormitorySaved"
    />
    <RoomDialog
      v-model:open="roomDialogOpen"
      :room="roomDialogTarget"
      @saved="refreshDormitory"
    />
    <BulkRoomDialog
      v-model:open="bulkRoomDialogOpen"
      :dormitory-id="selectedDormitory?.id ?? ''"
      :dormitory-name="selectedDormitory?.name ?? ''"
      @generated="refreshDormitory"
    />
    <DeleteConfirmDialog
      v-model:open="deleteOpen"
      :target-name="deleteName"
      :error="deleteError"
      :loading="deleteLoading"
      @confirmed="onDeleteConfirmed"
    />
  </AppLayout>
</template>
