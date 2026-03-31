<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
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
import { Upload, Users, AlertCircle } from 'lucide-vue-next'
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
const uploadResult = ref<UploadCsvResult | null>(null)
const uploadError = ref('')
const uploading = ref(false)
const fileInput = ref<HTMLInputElement | null>(null)

const selectedPeriod = () => periods.value.find(p => p.id === selectedPeriodId.value)
const isDraft = () => selectedPeriod()?.status === 'Draft'

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
  try {
    students.value = await getStudentRecords(periodId)
  } catch {
    students.value = []
  } finally {
    loadingStudents.value = false
  }
})

function triggerUpload() {
  fileInput.value?.click()
}

async function handleFileSelected(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !selectedPeriodId.value) return

  uploading.value = true
  uploadError.value = ''
  uploadResult.value = null
  try {
    const result = await uploadStudentRecordsCsv(selectedPeriodId.value, file)
    uploadResult.value = result
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
    input.value = ''
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-lg font-semibold tracking-tight">Students</h1>
          <p class="text-xs text-muted-foreground">Upload and manage student lists per allocation period.</p>
        </div>
      </div>

      <!-- Loading -->
      <Skeleton v-if="loading" class="h-10 w-64" />

      <template v-else>
        <!-- Period selector + Upload -->
        <div class="flex flex-wrap items-end gap-3">
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

          <Button
            v-if="isDraft()"
            size="sm"
            :disabled="uploading"
            @click="triggerUpload"
          >
            <Upload class="mr-1.5 size-3.5" />
            {{ uploading ? 'Uploading...' : 'Upload CSV' }}
          </Button>
          <Badge v-if="!isDraft() && selectedPeriodId" variant="secondary" class="h-7 text-xs">
            CSV upload only available in Draft status
          </Badge>

          <input
            ref="fileInput"
            type="file"
            accept=".csv"
            class="hidden"
            @change="handleFileSelected"
          />
        </div>

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
            {{ isDraft() ? 'Upload a CSV to import students.' : 'No students uploaded for this period.' }}
          </p>
        </div>

        <div v-else-if="students.length > 0" class="rounded-lg border">
          <!-- Table header -->
          <div class="grid grid-cols-[1fr_1fr_120px_80px_100px] gap-2 border-b px-3 py-2 text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">
            <span>Name</span>
            <span>Matriculation Code</span>
            <span>Faculty</span>
            <span class="text-right">Points</span>
            <span class="text-right">Status</span>
          </div>
          <!-- Table rows -->
          <div
            v-for="student in students"
            :key="student.id"
            class="grid grid-cols-[1fr_1fr_120px_80px_100px] gap-2 px-3 py-1.5 text-sm transition-colors hover:bg-muted/50"
          >
            <span class="truncate">{{ student.firstName }} {{ student.lastName }}</span>
            <span class="font-mono text-xs">{{ student.matriculationCode }}</span>
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
