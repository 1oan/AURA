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
import { Plus, Pencil, Trash2, GraduationCap } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import {
  getFaculties,
  createFaculty,
  updateFaculty,
  deleteFaculty,
} from '@/api/faculties'
import type { FacultyDto } from '@/api/faculties'

// --- State ---
const faculties = ref<FacultyDto[]>([])
const loading = ref(true)

// --- Dialog ---
const dialogOpen = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const form = ref({ id: '', name: '', abbreviation: '' })
const formError = ref('')
const formSaving = ref(false)

// --- Delete ---
const deleteDialogOpen = ref(false)
const deleteTarget = ref<{ id: string; name: string }>({ id: '', name: '' })
const deleteError = ref('')
const deleteLoading = ref(false)

// --- Data Fetching ---
onMounted(async () => {
  try {
    faculties.value = await getFaculties()
  } catch (e) {
    console.error('Failed to load faculties:', e)
  } finally {
    loading.value = false
  }
})

// --- CRUD ---
function openCreate() {
  dialogMode.value = 'create'
  form.value = { id: '', name: '', abbreviation: '' }
  formError.value = ''
  dialogOpen.value = true
}

function openEdit(faculty: FacultyDto) {
  dialogMode.value = 'edit'
  form.value = { id: faculty.id, name: faculty.name, abbreviation: faculty.abbreviation }
  formError.value = ''
  dialogOpen.value = true
}

async function save() {
  formError.value = ''
  if (!form.value.name.trim()) {
    formError.value = 'Name is required.'
    return
  }
  if (!form.value.abbreviation.trim()) {
    formError.value = 'Abbreviation is required.'
    return
  }
  formSaving.value = true
  try {
    const payload = {
      name: form.value.name.trim(),
      abbreviation: form.value.abbreviation.trim(),
    }
    if (dialogMode.value === 'create') {
      const created = await createFaculty(payload)
      faculties.value.push(created)
      toast.success('Faculty created successfully.')
    } else {
      await updateFaculty(form.value.id, payload)
      const idx = faculties.value.findIndex((f) => f.id === form.value.id)
      if (idx !== -1) {
        faculties.value[idx] = { id: faculties.value[idx]!.id, ...payload }
      }
      toast.success('Faculty updated successfully.')
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

function confirmDelete(faculty: FacultyDto) {
  deleteTarget.value = { id: faculty.id, name: faculty.name }
  deleteError.value = ''
  deleteDialogOpen.value = true
}

async function executeDelete() {
  deleteError.value = ''
  deleteLoading.value = true
  try {
    await deleteFaculty(deleteTarget.value.id)
    faculties.value = faculties.value.filter((f) => f.id !== deleteTarget.value.id)
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
</script>

<template>
  <AppLayout>
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-3xl font-bold tracking-tight">Faculties</h1>
          <p class="mt-1 text-muted-foreground">
            Manage faculties and assign admins.
          </p>
        </div>
        <Button @click="openCreate">
          <Plus class="mr-2 size-4" />
          Add Faculty
        </Button>
      </div>

      <!-- Loading skeleton -->
      <div v-if="loading" class="space-y-3">
        <Skeleton class="h-14 w-full" />
        <Skeleton class="h-14 w-full" />
        <Skeleton class="h-14 w-full" />
      </div>

      <!-- Empty state -->
      <div
        v-else-if="faculties.length === 0"
        class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
      >
        <GraduationCap class="mb-4 size-12 text-muted-foreground" />
        <p class="text-lg font-medium">No faculties yet</p>
        <p class="mb-4 text-sm text-muted-foreground">Get started by adding your first faculty.</p>
        <Button @click="openCreate">
          <Plus class="mr-2 size-4" />
          Add Faculty
        </Button>
      </div>

      <!-- Faculty list -->
      <div v-else class="rounded-lg border">
        <div
          v-for="(faculty, index) in faculties"
          :key="faculty.id"
          class="flex items-center justify-between px-4 py-3"
          :class="{ 'border-t': index > 0 }"
        >
          <div class="flex items-center gap-3">
            <GraduationCap class="size-5 shrink-0 text-muted-foreground" />
            <span class="font-medium">{{ faculty.name }}</span>
            <Badge variant="outline">{{ faculty.abbreviation }}</Badge>
          </div>
          <div class="flex items-center gap-1">
            <Button variant="ghost" size="icon" class="size-8" @click="openEdit(faculty)">
              <Pencil class="size-4" />
            </Button>
            <Button variant="ghost" size="icon" class="size-8" @click="confirmDelete(faculty)">
              <Trash2 class="size-4" />
            </Button>
          </div>
        </div>
      </div>
    </div>

    <!-- Create/Edit Dialog -->
    <Dialog v-model:open="dialogOpen">
      <DialogContent class="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{{ dialogMode === 'create' ? 'Add Faculty' : 'Edit Faculty' }}</DialogTitle>
          <DialogDescription>
            {{ dialogMode === 'create' ? 'Create a new faculty.' : 'Update faculty details.' }}
          </DialogDescription>
        </DialogHeader>
        <form class="space-y-4" @submit.prevent="save">
          <div class="space-y-2">
            <Label for="faculty-name">Name</Label>
            <Input id="faculty-name" v-model="form.name" placeholder="e.g. Computer Science" required />
          </div>
          <div class="space-y-2">
            <Label for="faculty-abbreviation">Abbreviation</Label>
            <Input id="faculty-abbreviation" v-model="form.abbreviation" placeholder="e.g. CS" required />
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
            This will permanently delete <strong>{{ deleteTarget.name }}</strong>.
            The faculty must have no room allocations before it can be deleted.
            This action cannot be undone.
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
  </AppLayout>
</template>
