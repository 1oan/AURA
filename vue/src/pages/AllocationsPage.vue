<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { toast } from 'vue-sonner'
import { useAuthStore } from '@/stores/auth'
import AppLayout from '@/components/layout/AppLayout.vue'
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
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Play, Loader, Inbox } from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { getAllocationsForPeriod, advanceRound } from '@/api/dormAllocations'
import type { DormAllocationDto } from '@/api/dormAllocations'

// --- State ---
const authStore = useAuthStore()
const isSuperAdmin = computed(() => authStore.user?.role === 'SuperAdmin')

const periods = ref<AllocationPeriodDto[]>([])
const selectedPeriodId = ref<string>('')
const allocations = ref<DormAllocationDto[]>([])
const periodsLoading = ref(true)
const allocationsLoading = ref(false)

const selectedPeriod = computed(() => periods.value.find(p => p.id === selectedPeriodId.value) ?? null)

// --- Run Next Round dialog ---
const advanceDialogOpen = ref(false)
const advanceLoading = ref(false)

// --- Data fetching ---
onMounted(async () => {
  try {
    const all = await getAllocationPeriods()
    periods.value = all
    const allocating = all.find(p => p.status === 'Allocating')
    if (allocating) {
      selectedPeriodId.value = allocating.id
    } else if (all.length > 0) {
      selectedPeriodId.value = all[0]!.id
    }
  } catch (e) {
    console.error('Failed to load allocation periods:', e)
  } finally {
    periodsLoading.value = false
  }
})

watch(selectedPeriodId, async (id) => {
  if (!id) return
  allocationsLoading.value = true
  try {
    allocations.value = await getAllocationsForPeriod(id)
  } catch (e) {
    console.error('Failed to load allocations:', e)
    allocations.value = []
  } finally {
    allocationsLoading.value = false
  }
})

// --- Run Next Round ---
async function executeAdvanceRound() {
  if (!selectedPeriodId.value) return
  advanceLoading.value = true
  try {
    await advanceRound(selectedPeriodId.value)
    allocations.value = await getAllocationsForPeriod(selectedPeriodId.value)
    advanceDialogOpen.value = false
    toast.success('Allocation round completed successfully.')
  } catch (e) {
    advanceDialogOpen.value = false
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Failed to run the allocation round.')
    } else {
      toast.error('Failed to run the allocation round.')
    }
  } finally {
    advanceLoading.value = false
  }
}

// --- Helpers ---
function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', { month: 'short', day: 'numeric', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}

function periodStatusVariant(status: string) {
  if (status === 'Open') return 'default' as const
  if (status === 'Allocating') return 'default' as const
  if (status === 'Closed') return 'secondary' as const
  return 'outline' as const
}

function allocationStatusVariant(status: string) {
  if (status === 'Accepted') return 'default' as const
  if (status === 'Declined') return 'destructive' as const
  return 'outline' as const
}

function allocationStatusClass(status: string): string {
  if (status === 'Accepted') return 'bg-emerald-500/10 text-emerald-700 border-emerald-300 dark:text-emerald-400 dark:border-emerald-700'
  if (status === 'Declined') return ''
  if (status === 'Pending') return 'bg-amber-500/10 text-amber-700 border-amber-300 dark:text-amber-400 dark:border-amber-700'
  return ''
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-lg font-semibold tracking-tight">Allocations</h1>
          <p class="text-xs text-muted-foreground">View and manage dormitory allocations per period.</p>
        </div>
      </div>

      <!-- Loading periods skeleton -->
      <div v-if="periodsLoading" class="space-y-2">
        <Skeleton class="h-10 w-64" />
        <Skeleton class="h-24" />
        <Skeleton v-for="i in 4" :key="i" class="h-10" />
      </div>

      <template v-else-if="periods.length === 0">
        <div class="flex flex-col items-center justify-center rounded-lg border border-dashed py-12">
          <Inbox class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No allocation periods found</p>
          <p class="text-xs text-muted-foreground">Create an allocation period first.</p>
        </div>
      </template>

      <template v-else>
        <!-- Period selector + status card -->
        <div class="flex flex-wrap items-start gap-3">
          <!-- Period selector -->
          <Select v-model="selectedPeriodId">
            <SelectTrigger class="w-56">
              <SelectValue placeholder="Select a period" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem v-for="period in periods" :key="period.id" :value="period.id">
                {{ period.name }}
              </SelectItem>
            </SelectContent>
          </Select>
        </div>

        <!-- Selected period info card -->
        <Card v-if="selectedPeriod">
          <CardHeader class="p-3 pb-2">
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <CardTitle class="text-sm font-medium">{{ selectedPeriod.name }}</CardTitle>
                <Badge :variant="periodStatusVariant(selectedPeriod.status)" class="h-4 px-1.5 text-[9px]">
                  {{ selectedPeriod.status }}
                </Badge>
              </div>
              <Button
                v-if="isSuperAdmin"
                size="sm"
                class="h-7 text-xs"
                :disabled="selectedPeriod.status !== 'Allocating'"
                @click="advanceDialogOpen = true"
              >
                <Play class="mr-1.5 size-3" />
                Run Next Round
              </Button>
            </div>
          </CardHeader>
          <CardContent class="p-3 pt-0">
            <p class="font-mono text-[11px] text-muted-foreground">
              {{ formatDate(selectedPeriod.startDate) }} — {{ formatDate(selectedPeriod.endDate) }}
            </p>
          </CardContent>
        </Card>

        <!-- Allocations table -->
        <div v-if="allocationsLoading" class="space-y-2">
          <Skeleton v-for="i in 5" :key="i" class="h-10" />
        </div>

        <template v-else-if="allocations.length === 0">
          <div class="flex flex-col items-center justify-center rounded-lg border border-dashed py-12">
            <Inbox class="mb-3 size-10 text-muted-foreground" />
            <p class="text-sm font-medium">No allocations yet for this period.</p>
            <p class="text-xs text-muted-foreground">Run the allocation algorithm to generate results.</p>
          </div>
        </template>

        <div v-else class="overflow-hidden rounded-lg border">
          <table class="w-full text-sm">
            <thead class="border-b bg-muted/30">
              <tr>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Student</th>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Dormitory</th>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Campus</th>
                <th class="px-3 py-2 text-center text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Round</th>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Status</th>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Allocated At</th>
                <th class="px-3 py-2 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">Responded At</th>
              </tr>
            </thead>
            <tbody class="divide-y">
              <tr v-for="alloc in allocations" :key="alloc.id" class="hover:bg-muted/20 transition-colors">
                <td class="px-3 py-2">
                  <div>
                    <span
                      class="text-sm font-medium"
                      :class="{ 'line-through text-muted-foreground': alloc.status === 'Replaced' }"
                    >
                      {{ alloc.userFirstName || alloc.userLastName
                        ? `${alloc.userFirstName ?? ''} ${alloc.userLastName ?? ''}`.trim()
                        : '—' }}
                    </span>
                    <p v-if="alloc.userMatriculationCode" class="font-mono text-[10px] text-muted-foreground">
                      {{ alloc.userMatriculationCode }}
                    </p>
                  </div>
                </td>
                <td class="px-3 py-2 text-sm">{{ alloc.dormitoryName }}</td>
                <td class="px-3 py-2 text-sm text-muted-foreground">{{ alloc.campusName }}</td>
                <td class="px-3 py-2 text-center font-mono text-sm">{{ alloc.round }}</td>
                <td class="px-3 py-2">
                  <Badge
                    :variant="allocationStatusVariant(alloc.status)"
                    :class="allocationStatusClass(alloc.status)"
                    class="h-5 px-1.5 text-[10px]"
                  >
                    <span :class="{ 'line-through': alloc.status === 'Replaced' }">{{ alloc.status }}</span>
                  </Badge>
                </td>
                <td class="px-3 py-2 font-mono text-xs text-muted-foreground">{{ formatDateTime(alloc.allocatedAt) }}</td>
                <td class="px-3 py-2 font-mono text-xs text-muted-foreground">
                  {{ alloc.respondedAt ? formatDateTime(alloc.respondedAt) : '—' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </div>

    <!-- Run Next Round confirmation dialog -->
    <AlertDialog v-model:open="advanceDialogOpen">
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Run Allocation Round?</AlertDialogTitle>
          <AlertDialogDescription>
            This will run the next allocation round for
            <strong>{{ selectedPeriod?.name }}</strong>,
            assigning dormitories to
            {{ allocations.filter(a => a.status === 'Declined' || a.status === 'Expired').length || 'eligible' }}
            students. Are you sure?
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel :disabled="advanceLoading">Cancel</AlertDialogCancel>
          <Button :disabled="advanceLoading" @click="executeAdvanceRound">
            <Loader v-if="advanceLoading" class="mr-1.5 size-3.5 animate-spin" />
            {{ advanceLoading ? 'Running...' : 'Run Round' }}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  </AppLayout>
</template>
