<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import { toast } from 'vue-sonner'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Upload, Users, AlertCircle, FileText, X } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { uploadStudentRecordsCsv, getStudentRecords } from '@/api/studentRecords'
import type { StudentRecordDto, UploadCsvResult } from '@/api/studentRecords'

const periods = ref<AllocationPeriodDto[]>([])
const selectedPeriodId = ref('')
const students = ref<StudentRecordDto[]>([])
const loading = ref(true)
const loadingStudents = ref(false)

// Upload state
const selectedFile = ref<File | null>(null)
const uploadResult = ref<UploadCsvResult | null>(null)
const uploadError = ref('')
const uploading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)
const isDragging = ref(false)

const selectedPeriod = computed(() => periods.value.find(p => p.id === selectedPeriodId.value))
const isDraft = computed(() => selectedPeriod.value?.status === 'Draft')

const fileSize = computed(() => {
  if (!selectedFile.value) return ''
  const bytes = selectedFile.value.size
  if (bytes < 1024) return `${bytes} B`
  return `${(bytes / 1024).toFixed(1)} KB`
})

onMounted(async () => {
  try {
    periods.value = await getAllocationPeriods()
    if (periods.value.length > 0) {
      selectedPeriodId.value = periods.value[0]!.id
    }
  } catch {
    // silent
  } finally {
    loading.value = false
  }
})

watch(selectedPeriodId, async (periodId) => {
  if (!periodId) return
  loadingStudents.value = true
  uploadResult.value = null
  uploadError.value = ''
  selectedFile.value = null
  try {
    students.value = await getStudentRecords(periodId)
  } catch {
    students.value = []
  } finally {
    loadingStudents.value = false
  }
})

function triggerFileSelect() {
  fileInput.value?.click()
}

function handleFileSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (file) {
    selectedFile.value = file
    uploadResult.value = null
    uploadError.value = ''
  }
  input.value = ''
}

function clearFile() {
  selectedFile.value = null
  uploadResult.value = null
  uploadError.value = ''
}

function handleDragOver(event: DragEvent) {
  event.preventDefault()
  isDragging.value = true
}

function handleDragLeave() {
  isDragging.value = false
}

function handleDrop(event: DragEvent) {
  event.preventDefault()
  isDragging.value = false
  const file = event.dataTransfer?.files[0]
  if (file && file.name.endsWith('.csv')) {
    selectedFile.value = file
    uploadResult.value = null
    uploadError.value = ''
  }
}

async function handleUpload() {
  if (!selectedFile.value || !selectedPeriodId.value) return

  uploading.value = true
  uploadError.value = ''
  uploadResult.value = null
  try {
    const result = await uploadStudentRecordsCsv(selectedPeriodId.value, selectedFile.value)
    uploadResult.value = result
    selectedFile.value = null
    if (result.errors.length === 0) {
      toast.success(`${result.created} student records imported.`)
    } else {
      toast.warning(`${result.created} imported, ${result.errors.length} errors.`)
    }
    students.value = await getStudentRecords(selectedPeriodId.value)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      uploadError.value = data.detail ?? 'Upload failed.'
    }
  } finally {
    uploading.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <!-- Header -->
      <div>
        <h1 class="text-lg font-semibold tracking-tight">Students</h1>
        <p class="text-xs text-muted-foreground">Upload and manage student lists per allocation period.</p>
      </div>

      <!-- Loading -->
      <Skeleton v-if="loading" class="h-10 w-64" />

      <template v-else>
        <!-- Period selector -->
        <div class="w-48 space-y-1">
          <label class="text-xs font-medium">Period</label>
          <Select v-model="selectedPeriodId">
            <SelectTrigger>
              <SelectValue placeholder="Select period" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="p in periods" :key="p.id" :value="p.id">
                {{ p.name }}
                <Badge :variant="p.status === 'Draft' ? 'outline' : 'secondary'" class="ml-1.5 text-[9px]">
                  {{ p.status }}
                </Badge>
              </SelectItem>
            </SelectContent>
          </Select>
        </div>

        <!-- Upload zone (Draft only) -->
        <div v-if="isDraft" class="space-y-2">
          <!-- Drop zone / file selection -->
          <div
            v-if="!selectedFile"
            class="flex cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed px-4 py-8 transition-colors"
            :class="isDragging ? 'border-primary bg-primary/5' : 'border-muted-foreground/25 hover:border-primary/50'"
            @click="triggerFileSelect"
            @dragover="handleDragOver"
            @dragleave="handleDragLeave"
            @drop="handleDrop"
          >
            <Upload class="mb-2 size-8 text-muted-foreground" />
            <p class="text-sm font-medium">Drop a CSV file here or click to browse</p>
            <p class="text-xs text-muted-foreground">Format: FirstName, LastName, MatriculationCode, Points, Gender</p>
          </div>

          <!-- File preview + upload button -->
          <Card v-else class="border-primary/30">
            <CardContent class="flex items-center gap-3 p-3">
              <FileText class="size-8 shrink-0 text-primary" />
              <div class="min-w-0 flex-1">
                <p class="truncate text-sm font-medium">{{ selectedFile.name }}</p>
                <p class="text-xs text-muted-foreground">{{ fileSize }}</p>
                <p v-if="students.length > 0" class="text-xs text-amber-500">
                  This will replace {{ students.length }} existing records for your faculty.
                </p>
              </div>
              <div class="flex items-center gap-1.5">
                <Button size="sm" :disabled="uploading" @click="handleUpload">
                  <Upload class="mr-1.5 size-3.5" />
                  {{ uploading ? 'Uploading...' : 'Upload' }}
                </Button>
                <Button variant="ghost" size="icon" class="size-8" :disabled="uploading" @click="clearFile">
                  <X class="size-4" />
                </Button>
              </div>
            </CardContent>
          </Card>

          <input
            ref="fileInput"
            type="file"
            accept=".csv"
            class="hidden"
            @change="handleFileSelected"
          />
        </div>

        <Badge v-else-if="selectedPeriodId" variant="secondary" class="text-xs">
          CSV upload only available in Draft status
        </Badge>

        <!-- Upload result -->
        <Card v-if="uploadResult" class="border-primary/20 bg-primary/5">
          <CardContent class="p-3">
            <p class="text-sm font-medium">
              {{ uploadResult.created }} records imported
              <span v-if="uploadResult.errors.length > 0" class="text-destructive">
                , {{ uploadResult.errors.length }} errors
              </span>
            </p>
            <div v-if="uploadResult.errors.length > 0" class="mt-2 space-y-1">
              <p v-for="err in uploadResult.errors" :key="err.row" class="text-xs text-destructive">
                <span class="font-mono">Row {{ err.row }}:</span> {{ err.message }}
              </p>
            </div>
          </CardContent>
        </Card>

        <!-- Upload error -->
        <Card v-if="uploadError" class="border-destructive/20 bg-destructive/5">
          <CardContent class="flex items-center gap-2 p-3">
            <AlertCircle class="size-4 text-destructive" />
            <p role="alert" class="text-sm text-destructive">{{ uploadError }}</p>
          </CardContent>
        </Card>

        <!-- Student list -->
        <div v-if="loadingStudents" class="space-y-1">
          <Skeleton v-for="i in 5" :key="i" class="h-9" />
        </div>

        <div
          v-else-if="students.length === 0 && selectedPeriodId"
          class="flex flex-col items-center justify-center rounded-lg border border-dashed py-12"
        >
          <Users class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No student records</p>
          <p class="text-xs text-muted-foreground">
            {{ isDraft ? 'Upload a CSV to import students.' : 'No students uploaded for this period.' }}
          </p>
        </div>

        <div v-else-if="students.length > 0" class="rounded-lg border">
          <!-- Table header -->
          <div class="grid grid-cols-[1fr_1fr_80px_80px_80px_90px] gap-2 border-b px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
            <span>Name</span>
            <span>Matriculation Code</span>
            <span>Gender</span>
            <span>Faculty</span>
            <span class="text-right">Points</span>
            <span class="text-right">Status</span>
          </div>
          <!-- Table rows -->
          <div
            v-for="student in students"
            :key="student.id"
            class="grid grid-cols-[1fr_1fr_80px_80px_80px_90px] gap-2 px-3 py-1.5 text-sm transition-colors hover:bg-muted/50"
          >
            <span class="truncate">{{ student.firstName }} {{ student.lastName }}</span>
            <span class="font-mono text-xs">{{ student.matriculationCode }}</span>
            <Badge :variant="student.gender === 'Female' ? 'secondary' : 'outline'" class="w-fit text-[10px]">
              {{ student.gender }}
            </Badge>
            <Badge variant="outline" class="w-fit font-mono text-[10px]">{{ student.facultyAbbreviation }}</Badge>
            <span class="text-right font-mono text-xs tabular-nums">{{ student.points.toFixed(2) }}</span>
            <span class="text-right">
              <Badge v-if="student.userId" variant="default" class="text-[9px]">Linked</Badge>
              <Badge v-else variant="secondary" class="text-[9px]">Pending</Badge>
            </span>
          </div>
        </div>
      </template>
    </div>
  </AppLayout>
</template>
