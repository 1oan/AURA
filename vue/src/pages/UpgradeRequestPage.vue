<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { toast } from 'vue-sonner'
import draggable from 'vuedraggable'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import {
  ArrowLeft,
  ArrowUpFromLine,
  ChevronDown,
  ChevronUp,
  GripVertical,
  Building2,
  Home,
  Loader2,
} from 'lucide-vue-next'
import { ApiError } from '@/api/client'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { getMyAllocation } from '@/api/dormAllocations'
import type { DormAllocationDto } from '@/api/dormAllocations'
import {
  getAvailableUpgradeTargets,
  getMyUpgradeRequest,
  submitUpgradeRequest,
} from '@/api/upgradeRequests'
import type {
  AvailableUpgradeTargetDto,
  UpgradeRequestDto,
} from '@/api/upgradeRequests'

const router = useRouter()

const loading = ref(true)
const saving = ref(false)
const activePeriod = ref<AllocationPeriodDto | null>(null)
const myAllocation = ref<DormAllocationDto | null>(null)
const availableTargets = ref<AvailableUpgradeTargetDto[]>([])
const existingRequest = ref<UpgradeRequestDto | null>(null)
const rankedTargets = ref<AvailableUpgradeTargetDto[]>([])
const initialRankedIds = ref<string[]>([])

const isDirty = computed(() => {
  const current = rankedTargets.value.map(t => t.dormitoryId)
  if (current.length !== initialRankedIds.value.length) return true
  return current.some((id, i) => id !== initialRankedIds.value[i])
})

const isEligible = computed(() =>
  myAllocation.value !== null && myAllocation.value.status === 'Accepted',
)

const showEmptyState = computed(() =>
  !loading.value && availableTargets.value.length === 0,
)

const unrankedTargets = computed(() =>
  availableTargets.value.filter(t =>
    !rankedTargets.value.some(r => r.dormitoryId === t.dormitoryId),
  ),
)

onMounted(async () => {
  try {
    const periods = await getAllocationPeriods()
    const allocating = periods.find(p => p.status === 'Allocating')
    if (!allocating) {
      loading.value = false
      return
    }
    activePeriod.value = allocating

    const [allocation, targets, existing] = await Promise.all([
      getMyAllocation(allocating.id),
      getAvailableUpgradeTargets(allocating.id).catch((e) => {
        if (e instanceof ApiError) return [] as AvailableUpgradeTargetDto[]
        throw e
      }),
      getMyUpgradeRequest(allocating.id).catch(() => null),
    ])

    myAllocation.value = allocation
    availableTargets.value = targets
    existingRequest.value = existing

    if (existing) {
      const targetMap = new Map(targets.map(t => [t.dormitoryId, t]))
      rankedTargets.value = existing.targets
        .filter(et => targetMap.has(et.dormitoryId))
        .map(et => targetMap.get(et.dormitoryId)!)
      initialRankedIds.value = rankedTargets.value.map(t => t.dormitoryId)
    } else {
      rankedTargets.value = []
      initialRankedIds.value = []
    }
  } catch {
    toast.error('Could not load upgrade options.')
  } finally {
    loading.value = false
  }
})

function addToRanked(target: AvailableUpgradeTargetDto) {
  rankedTargets.value.push(target)
}

function removeFromRanked(target: AvailableUpgradeTargetDto) {
  rankedTargets.value = rankedTargets.value.filter(r => r.dormitoryId !== target.dormitoryId)
}

function moveUp(index: number) {
  if (index === 0) return
  const arr = [...rankedTargets.value]
  ;[arr[index - 1], arr[index]] = [arr[index]!, arr[index - 1]!]
  rankedTargets.value = arr
}

function moveDown(index: number) {
  if (index === rankedTargets.value.length - 1) return
  const arr = [...rankedTargets.value]
  ;[arr[index + 1], arr[index]] = [arr[index]!, arr[index + 1]!]
  rankedTargets.value = arr
}

async function handleSave() {
  if (!activePeriod.value || rankedTargets.value.length === 0) return
  saving.value = true
  try {
    await submitUpgradeRequest({
      allocationPeriodId: activePeriod.value.id,
      dormitoryIds: rankedTargets.value.map(t => t.dormitoryId),
    })
    toast.success('Upgrade request saved.')
    initialRankedIds.value = rankedTargets.value.map(t => t.dormitoryId)
    router.push('/')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not save the upgrade request.')
    } else {
      toast.error('Could not save the upgrade request.')
    }
  } finally {
    saving.value = false
  }
}

function handleCancel() {
  router.push('/')
}
</script>

<template>
  <AppLayout>
    <div class="space-y-4 pb-24">
      <div class="flex items-center gap-2">
        <Button variant="ghost" size="sm" class="-ml-2 h-8 px-2" @click="handleCancel">
          <ArrowLeft class="mr-1 size-4" />
          Dashboard
        </Button>
      </div>

      <div>
        <h1 class="text-3xl font-semibold tracking-tight leading-none">Upgrade request</h1>
        <p class="mt-2 text-sm text-muted-foreground max-w-[65ch]">
          Pick the dorms you would prefer over your current placement, in priority order.
          When a spot opens in one of your targets, the system will move you automatically.
        </p>
      </div>

      <div v-if="loading" class="space-y-4">
        <Skeleton class="h-20" />
        <Skeleton class="h-12" />
        <Skeleton class="h-12" />
        <Skeleton class="h-12" />
      </div>

      <Card v-else-if="!isEligible">
        <CardContent class="flex flex-col items-center justify-center py-12 text-center">
          <Building2 class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">Upgrade requests are not available</p>
          <p class="text-xs text-muted-foreground max-w-md mt-1">
            You need an accepted allocation in this period before you can request an upgrade.
          </p>
          <Button variant="outline" size="sm" class="mt-4 h-7 text-xs" @click="handleCancel">
            Back to dashboard
          </Button>
        </CardContent>
      </Card>

      <Card v-else-if="showEmptyState">
        <CardContent class="flex flex-col items-center justify-center py-12 text-center">
          <Building2 class="mb-3 size-10 text-muted-foreground" />
          <p class="text-sm font-medium">No upgrade targets available</p>
          <p class="text-xs text-muted-foreground max-w-md mt-1">
            Your faculty has rooms in only one dorm matching your gender — the one where you already are.
            If your faculty is allocated more dorms in this period, you will be able to upgrade then.
          </p>
          <Button variant="outline" size="sm" class="mt-4 h-7 text-xs" @click="handleCancel">
            Back to dashboard
          </Button>
        </CardContent>
      </Card>

      <template v-else-if="myAllocation">
        <Card class="border-primary/20 bg-primary/5">
          <CardContent class="p-4">
            <div class="flex items-center gap-3">
              <Home class="size-5 text-primary" />
              <div>
                <p class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Current placement</p>
                <p class="text-sm font-medium mt-0.5">
                  {{ myAllocation.dormitoryName }}<span v-if="myAllocation.dormitoryName !== myAllocation.campusName"> in {{ myAllocation.campusName }}</span>
                </p>
              </div>
            </div>
          </CardContent>
        </Card>

        <div>
          <h2 class="text-sm font-semibold tracking-tight mb-2">Your upgrade priorities</h2>
          <div v-if="rankedTargets.length === 0" class="rounded-lg border border-dashed p-8 text-center">
            <ArrowUpFromLine class="mx-auto mb-2 size-6 text-muted-foreground" />
            <p class="text-xs text-muted-foreground">
              Pick at least one dorm from the list below.
            </p>
          </div>
          <div v-else class="rounded-lg border divide-y">
            <draggable
              v-model="rankedTargets"
              :animation="200"
              ghost-class="opacity-50"
              handle=".drag-handle"
              item-key="dormitoryId"
              tag="div"
            >
              <template #item="{ element, index }">
                <div class="flex items-center gap-3 px-3 py-2.5 hover:bg-muted/40 transition-colors">
                  <span class="drag-handle cursor-grab active:cursor-grabbing text-muted-foreground">
                    <GripVertical class="size-4" />
                  </span>
                  <span class="font-mono text-sm font-medium w-6 text-center">{{ index + 1 }}</span>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium truncate">{{ element.dormitoryName }}</p>
                    <p v-if="element.dormitoryName !== element.campusName" class="text-xs text-muted-foreground truncate">
                      {{ element.campusName }}
                    </p>
                  </div>
                  <div class="flex items-center gap-1">
                    <Button
                      variant="ghost"
                      size="icon"
                      class="size-7"
                      :disabled="index === 0"
                      :aria-label="`Move ${element.dormitoryName} up`"
                      @click="moveUp(index)"
                    >
                      <ChevronUp class="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      class="size-7"
                      :disabled="index === rankedTargets.length - 1"
                      :aria-label="`Move ${element.dormitoryName} down`"
                      @click="moveDown(index)"
                    >
                      <ChevronDown class="size-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      class="h-7 text-xs"
                      @click="removeFromRanked(element)"
                    >
                      Remove
                    </Button>
                  </div>
                </div>
              </template>
            </draggable>
          </div>
        </div>

        <div v-if="unrankedTargets.length > 0">
          <h2 class="text-sm font-semibold tracking-tight mb-2">Available dorms</h2>
          <div class="rounded-lg border divide-y">
            <div
              v-for="target in unrankedTargets"
              :key="target.dormitoryId"
              class="flex items-center gap-3 px-3 py-2.5 hover:bg-muted/40 transition-colors"
            >
              <Building2 class="size-4 text-muted-foreground" />
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium truncate">{{ target.dormitoryName }}</p>
                <p v-if="target.dormitoryName !== target.campusName" class="text-xs text-muted-foreground truncate">
                  {{ target.campusName }}
                </p>
              </div>
              <Button
                variant="outline"
                size="sm"
                class="h-7 text-xs"
                @click="addToRanked(target)"
              >
                Add
              </Button>
            </div>
          </div>
        </div>

        <Transition
          enter-active-class="transition duration-200 ease-out"
          enter-from-class="translate-y-full opacity-0"
          enter-to-class="translate-y-0 opacity-100"
          leave-active-class="transition duration-150 ease-in"
          leave-from-class="translate-y-0 opacity-100"
          leave-to-class="translate-y-full opacity-0"
        >
          <div
            v-if="isDirty || rankedTargets.length > 0"
            class="fixed bottom-0 left-0 right-0 z-40 border-t bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/80"
          >
            <div class="mx-auto flex max-w-[1400px] items-center justify-end gap-2 px-4 py-3 sm:px-8">
              <p v-if="rankedTargets.length === 0" class="mr-auto text-xs text-muted-foreground">
                Pick at least one dorm to save your request.
              </p>
              <Button variant="outline" size="sm" :disabled="saving" @click="handleCancel">
                Cancel
              </Button>
              <Button
                size="sm"
                class="transition-transform active:scale-[0.98]"
                :disabled="saving || rankedTargets.length === 0"
                @click="handleSave"
              >
                <Loader2 v-if="saving" class="mr-2 size-4 animate-spin" />
                Save changes
              </Button>
            </div>
          </div>
        </Transition>
      </template>
    </div>
  </AppLayout>
</template>
