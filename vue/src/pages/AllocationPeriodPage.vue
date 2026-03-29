<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
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
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Plus, Pencil, Trash2, Calendar, Play, Square } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import {
  getAllocationPeriods,
  createAllocationPeriod,
  updateAllocationPeriod,
  activateAllocationPeriod,
  closeAllocationPeriod,
  deleteAllocationPeriod,
} from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'

// --- State ---
const periods = ref<AllocationPeriodDto[]>([])
const loading = ref(true)

// --- Dialog ---
const dialogOpen = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const form = ref({ id: '', name: '', startDate: '', endDate: '' })
const formError = ref('')
const formSaving = ref(false)

// --- Delete ---
const deleteDialogOpen = ref(false)
const deleteTarget = ref<{ id: string; name: string }>({ id: '', name: '' })
const deleteError = ref('')
const deleteLoading = ref(false)

// --- Activate / Close ---
const actionDialogOpen = ref(false)
const actionType = ref<'activate' | 'close'>('activate')
const actionTarget = ref<{ id: string; name: string }>({ id: '', name: '' })
const actionError = ref('')
const actionLoading = ref(false)

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function statusVariant(status: string) {
  if (status === 'Open') return 'default' as const
  if (status === 'Closed') return 'secondary' as const
  return 'outline' as const
}

// --- Data Fetching ---
onMounted(async () => {
  try {
    periods.value = await getAllocationPeriods()
  } catch (e) {
    console.error('Failed to load allocation periods:', e)
  } finally {
    loading.value = false
  }
})

// --- CRUD ---
function openCreate() {
  dialogMode.value = 'create'
  form.value = { id: '', name: '', startDate: '', endDate: '' }
  formError.value = ''
  dialogOpen.value = true
}

function openEdit(period: AllocationPeriodDto) {
  dialogMode.value = 'edit'
  form.value = {
    id: period.id,
    name: period.name,
    startDate: period.startDate.substring(0, 10),
    endDate: period.endDate.substring(0, 10),
  }
  formError.value = ''
  dialogOpen.value = true
}

async function save() {
  formError.value = ''
  if (!form.value.name.trim()) {
    formError.value = 'Name is required.'
    return
  }
  if (!form.value.startDate || !form.value.endDate) {
    formError.value = 'Start and end dates are required.'
    return
  }
  if (form.value.startDate >= form.value.endDate) {
    formError.value = 'End date must be after start date.'
    return
  }
  formSaving.value = true
  try {
    const payload = {
      name: form.value.name.trim(),
      startDate: form.value.startDate,
      endDate: form.value.endDate,
    }
    if (dialogMode.value === 'create') {
      const created = await createAllocationPeriod(payload)
      periods.value.push(created)
      toast.success('Period created successfully.')
    } else {
      await updateAllocationPeriod(form.value.id, payload)
      const idx = periods.value.findIndex((p) => p.id === form.value.id)
      if (idx !== -1) {
        periods.value[idx] = { ...periods.value[idx]!, ...payload }
      }
      toast.success('Period updated successfully.')
    }
    dialogOpen.value = false
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      formError.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    formSaving.value = false
  }
}

function confirmDelete(period: AllocationPeriodDto) {
  deleteTarget.value = { id: period.id, name: period.name }
  deleteError.value = ''
  deleteDialogOpen.value = true
}

async function executeDelete() {
  deleteError.value = ''
  deleteLoading.value = true
  try {
    await deleteAllocationPeriod(deleteTarget.value.id)
    periods.value = periods.value.filter((p) => p.id !== deleteTarget.value.id)
    deleteDialogOpen.value = false
    toast.success(`${deleteTarget.value.name} deleted.`)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      deleteError.value = data.detail ?? 'Failed to delete. It may have dependent records.'
    }
  } finally {
    deleteLoading.value = false
  }
}

// --- Activate / Close ---
function confirmAction(type: 'activate' | 'close', period: AllocationPeriodDto) {
  actionType.value = type
  actionTarget.value = { id: period.id, name: period.name }
  actionError.value = ''
  actionDialogOpen.value = true
}

async function executeAction() {
  actionError.value = ''
  actionLoading.value = true
  try {
    if (actionType.value === 'activate') {
      await activateAllocationPeriod(actionTarget.value.id)
    } else {
      await closeAllocationPeriod(actionTarget.value.id)
    }
    // Refetch to get updated statuses
    periods.value = await getAllocationPeriods()
    actionDialogOpen.value = false
    toast.success(actionType.value === 'activate' ? 'Period activated.' : 'Period closed.')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      actionError.value = data.detail ?? 'An unexpected error occurred.'
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
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-3xl font-bold tracking-tight">Allocation Periods</h1>
          <p class="mt-1 text-muted-foreground">
            Configure allocation timeline per year.
          </p>
        </div>
        <Button @click="openCreate">
          <Plus class="mr-2 size-4" />
          Create Period
        </Button>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-3">
        <Skeleton class="h-24 w-full" />
        <Skeleton class="h-24 w-full" />
      </div>

      <!-- Empty state -->
      <div
        v-else-if="periods.length === 0"
        class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
      >
        <Calendar class="mb-4 size-12 text-muted-foreground" />
        <p class="text-lg font-medium">No allocation periods yet</p>
        <p class="mb-4 text-sm text-muted-foreground">Create your first allocation period to get started.</p>
        <Button @click="openCreate">
          <Plus class="mr-2 size-4" />
          Create Period
        </Button>
      </div>

      <!-- Period list -->
      <div v-else class="space-y-3">
        <div
          v-for="period in periods"
          :key="period.id"
          class="rounded-lg border px-5 py-4"
        >
          <div class="flex items-start justify-between">
            <div class="space-y-1">
              <div class="flex items-center gap-3">
                <Calendar class="size-5 shrink-0 text-muted-foreground" />
                <h3 class="text-lg font-semibold">{{ period.name }}</h3>
                <Badge :variant="statusVariant(period.status)">{{ period.status }}</Badge>
              </div>
              <p class="pl-8 text-sm text-muted-foreground">
                {{ formatDate(period.startDate) }} &rarr; {{ formatDate(period.endDate) }}
              </p>
            </div>
            <div class="flex items-center gap-1">
              <template v-if="period.status === 'Draft'">
                <Button variant="outline" size="sm" @click="confirmAction('activate', period)">
                  <Play class="mr-1 size-4" />
                  Activate
                </Button>
                <Button variant="ghost" size="icon" class="size-8" @click="openEdit(period)">
                  <Pencil class="size-4" />
                </Button>
                <Button variant="ghost" size="icon" class="size-8" @click="confirmDelete(period)">
                  <Trash2 class="size-4" />
                </Button>
              </template>
              <template v-else-if="period.status === 'Open'">
                <Button variant="outline" size="sm" @click="confirmAction('close', period)">
                  <Square class="mr-1 size-4" />
                  Close
                </Button>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <Dialog v-model:open="dialogOpen">
      <DialogContent class="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{{ dialogMode === 'create' ? 'Create Period' : 'Edit Period' }}</DialogTitle>
          <DialogDescription>
            {{ dialogMode === 'create' ? 'Set up a new allocation period.' : 'Update period details.' }}
          </DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="save">
          <div class="space-y-2">
            <Label for="period-name">Name</Label>
            <Input id="period-name" v-model="form.name" placeholder="e.g. 2026-2027" required />
          </div>
          <div class="grid grid-cols-2 gap-4">
            <div class="space-y-2">
              <Label for="period-start">Start Date</Label>
              <Input id="period-start" v-model="form.startDate" type="date" required />
            </div>
            <div class="space-y-2">
              <Label for="period-end">End Date</Label>
              <Input id="period-end" v-model="form.endDate" type="date" required />
            </div>
          </div>
          <p v-if="formError" class="text-sm text-destructive">{{ formError }}</p>
          <DialogFooter>
            <Button type="submit" :disabled="formSaving">
              {{ formSaving ? 'Saving...' : 'Save' }}
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
            This will permanently delete <strong>{{ deleteTarget.name }}</strong>
            and all associated room allocations. This action cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <p v-if="deleteError" class="text-sm text-destructive">{{ deleteError }}</p>
        <AlertDialogFooter>
          <AlertDialogCancel :disabled="deleteLoading">Cancel</AlertDialogCancel>
          <Button
            variant="destructive"
            :disabled="deleteLoading"
            @click="executeDelete"
          >
            {{ deleteLoading ? 'Deleting...' : 'Delete' }}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>

    <!-- Activate / Close Confirmation -->
    <AlertDialog v-model:open="actionDialogOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>
            {{ actionType === 'activate' ? 'Activate Period?' : 'Close Period?' }}
          </AlertDialogTitle>
          <AlertDialogDescription>
            <template v-if="actionType === 'activate'">
              This will open <strong>{{ actionTarget.name }}</strong> for allocation.
              Only one period can be open at a time.
            </template>
            <template v-else>
              This will close <strong>{{ actionTarget.name }}</strong>.
              This action cannot be undone.
            </template>
          </AlertDialogDescription>
        </AlertDialogHeader>
        <p v-if="actionError" class="text-sm text-destructive">{{ actionError }}</p>
        <AlertDialogFooter>
          <AlertDialogCancel :disabled="actionLoading">Cancel</AlertDialogCancel>
          <Button
            :disabled="actionLoading"
            @click="executeAction"
          >
            {{ actionLoading ? 'Processing...' : (actionType === 'activate' ? 'Activate' : 'Close') }}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </AppLayout>
</template>
