<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { toast } from 'vue-sonner'
import draggable from 'vuedraggable'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { Building2, GripVertical, Save } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import {
  getAvailableDormitories,
  getMyPreferences,
  submitPreferences,
} from '@/api/dormPreferences'

interface RankedDorm {
  dormitoryId: string
  dormitoryName: string
  campusName: string
  availableSpots: number
}

const loading = ref(true)
const saving = ref(false)
const error = ref('')
const activePeriod = ref<AllocationPeriodDto | null>(null)
const rankedDorms = ref<RankedDorm[]>([])
const savedOrder = ref<string[]>([])
const hasExistingPreferences = ref(false)

// Campus color palette
const campusColors = [
  'bg-blue-500', 'bg-emerald-500', 'bg-amber-500', 'bg-purple-500',
  'bg-rose-500', 'bg-cyan-500', 'bg-orange-500', 'bg-indigo-500',
]
const campusColorMap = ref<Map<string, string>>(new Map())

function assignCampusColors(dorms: RankedDorm[]) {
  const campuses = [...new Set(dorms.map(d => d.campusName))]
  campuses.forEach((campus, i) => {
    campusColorMap.value.set(campus, campusColors[i % campusColors.length]!)
  })
}

function getCampusColor(campusName: string): string {
  return campusColorMap.value.get(campusName) ?? 'bg-muted-foreground'
}

const isOpen = computed(() =>
  activePeriod.value?.status === 'Open' || activePeriod.value?.status === 'Allocating'
)

const hasUnsavedChanges = computed(() => {
  if (rankedDorms.value.length === 0) return false
  const currentOrder = rankedDorms.value.map(d => d.dormitoryId)
  if (currentOrder.length !== savedOrder.value.length) return true
  return currentOrder.some((id, i) => id !== savedOrder.value[i])
})

onMounted(async () => {
  try {
    const periods = await getAllocationPeriods()
    activePeriod.value = periods.find(p => p.status === 'Open' || p.status === 'Allocating') ?? null

    if (!activePeriod.value) {
      loading.value = false
      return
    }

    const [available, existing] = await Promise.all([
      getAvailableDormitories(activePeriod.value.id),
      getMyPreferences(activePeriod.value.id),
    ])

    if (existing.length > 0) {
      hasExistingPreferences.value = true
      rankedDorms.value = existing.map(p => ({
        dormitoryId: p.dormitoryId,
        dormitoryName: p.dormitoryName,
        campusName: p.campusName,
        availableSpots: available.find(a => a.dormitoryId === p.dormitoryId)?.availableSpots ?? 0,
      }))
      savedOrder.value = existing.map(p => p.dormitoryId)
    } else {
      rankedDorms.value = available.map(a => ({
        dormitoryId: a.dormitoryId,
        dormitoryName: a.dormitoryName,
        campusName: a.campusName,
        availableSpots: a.availableSpots,
      }))
      savedOrder.value = []
    }

    assignCampusColors(rankedDorms.value)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Failed to load preferences.'
    }
  } finally {
    loading.value = false
  }
})

async function handleSubmit() {
  if (!activePeriod.value || rankedDorms.value.length === 0) return

  saving.value = true
  error.value = ''
  try {
    await submitPreferences(
      activePeriod.value.id,
      rankedDorms.value.map(d => d.dormitoryId),
    )
    hasExistingPreferences.value = true
    savedOrder.value = rankedDorms.value.map(d => d.dormitoryId)
    toast.success('Preferences saved successfully.')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Failed to save preferences.'
      toast.error(data.detail ?? 'Failed to save preferences.')
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-4">
      <div>
        <h1 class="text-lg font-semibold tracking-tight">Dormitory Preferences</h1>
        <p class="text-xs text-muted-foreground">
          Drag to reorder. The allocation algorithm will use this ranking.
        </p>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="space-y-3">
        <Skeleton class="h-10 w-full" />
        <Skeleton class="h-64 w-full" />
      </div>

      <!-- No active period -->
      <Card v-else-if="!activePeriod">
        <CardContent class="flex flex-col items-center justify-center py-12">
          <Building2 class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No active allocation period</p>
          <p class="text-xs text-muted-foreground">Preferences can only be submitted during an open allocation period.</p>
        </CardContent>
      </Card>

      <!-- Error -->
      <Card v-else-if="error && rankedDorms.length === 0">
        <CardContent class="py-8">
          <p class="text-center text-sm text-destructive">{{ error }}</p>
        </CardContent>
      </Card>

      <!-- No dormitories available -->
      <Card v-else-if="rankedDorms.length === 0">
        <CardContent class="flex flex-col items-center justify-center py-12">
          <Building2 class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No dormitories available</p>
          <p class="text-xs text-muted-foreground">Your faculty has no room allocations matching your gender for this period.</p>
        </CardContent>
      </Card>

      <!-- Preference ranking -->
      <template v-else>
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <Badge variant="outline">{{ activePeriod.name }}</Badge>
            <Badge v-if="hasExistingPreferences && !hasUnsavedChanges" variant="secondary" class="text-xs">Saved</Badge>
            <Badge v-if="hasUnsavedChanges" variant="outline" class="border-amber-500/50 text-xs text-amber-500">Unsaved changes</Badge>
          </div>
          <Button
            size="sm"
            :disabled="saving || rankedDorms.length === 0"
            @click="handleSubmit"
          >
            <Save class="mr-1.5 size-3.5" />
            {{ saving ? 'Saving...' : 'Save Preferences' }}
          </Button>
        </div>

        <p v-if="error" role="alert" class="text-sm text-destructive">{{ error }}</p>

        <!-- Campus legend -->
        <div class="flex flex-wrap gap-3">
          <div v-for="[campus, color] in campusColorMap" :key="campus" class="flex items-center gap-1.5">
            <span :class="[color, 'size-2.5 rounded-full']" />
            <span class="text-xs text-muted-foreground">{{ campus }}</span>
          </div>
        </div>

        <Card>
          <CardHeader class="p-3 pb-2">
            <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Your Ranking ({{ rankedDorms.length }} dormitories)
            </CardTitle>
          </CardHeader>
          <CardContent class="p-0">
            <draggable
              v-model="rankedDorms"
              item-key="dormitoryId"
              ghost-class="opacity-30"
              :disabled="!isOpen"
              class="divide-y"
            >
              <template #item="{ element: dorm, index }">
                <div class="flex cursor-grab select-none items-center gap-3 px-4 py-2.5 transition-colors hover:bg-muted/50 active:cursor-grabbing">
                  <!-- Rank number -->
                  <span class="flex size-7 shrink-0 items-center justify-center rounded-md bg-primary/10 text-xs font-bold text-primary">
                    {{ index + 1 }}
                  </span>

                  <!-- Drag handle -->
                  <GripVertical
                    v-if="isOpen"
                    class="size-4 shrink-0 text-muted-foreground/40"
                    aria-hidden="true"
                  />

                  <!-- Campus color dot -->
                  <span :class="[getCampusColor(dorm.campusName), 'size-2.5 shrink-0 rounded-full']" />

                  <!-- Dorm info -->
                  <div class="min-w-0 flex-1">
                    <p class="text-sm font-medium">{{ dorm.dormitoryName }}</p>
                    <p class="text-xs text-muted-foreground">{{ dorm.campusName }} &middot; {{ dorm.availableSpots }} spots</p>
                  </div>
                </div>
              </template>
            </draggable>
          </CardContent>
        </Card>
      </template>
    </div>
  </AppLayout>
</template>