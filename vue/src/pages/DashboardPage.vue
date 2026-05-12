<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { toast } from 'vue-sonner'
import {
  Building2,
  DoorOpen,
  BedDouble,
  CalendarClock,
  ArrowRight,
  Circle,
  CheckCircle2,
  CircleSlash2,
  GraduationCap,
  Home,
  Loader2,
  Users2,
} from 'lucide-vue-next'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuthStore } from '@/stores/auth'
import { getDashboardStats } from '@/api/dashboard'
import type { DashboardStatsDto } from '@/api/dashboard'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'
import { getMyEligibility, participate } from '@/api/studentRecords'
import type { MyEligibilityResult, ParticipateResult } from '@/api/studentRecords'
import { getMyAllocation, acceptAllocation, declineAllocation } from '@/api/dormAllocations'
import type { DormAllocationDto } from '@/api/dormAllocations'
import { getMyGroup } from '@/api/groups'
import type { GroupDto } from '@/api/groups'
import { getMyRoom } from '@/api/rooms'
import type { RoomAssignmentDto } from '@/api/rooms'
import { ApiError } from '@/api/client'
import DeclineAllocationDialog from '@/components/features/DeclineAllocationDialog.vue'
import ProfileCompletenessBanner from '@/components/features/profile/ProfileCompletenessBanner.vue'
import RoomCard from '@/components/features/lobby/RoomCard.vue'
import PlaceMeNowButton from '@/components/features/lobby/PlaceMeNowButton.vue'

const authStore = useAuthStore()
const isAdmin = computed(() => ['SuperAdmin', 'FacultyAdmin'].includes(authStore.user?.role ?? ''))

const stats = ref<DashboardStatsDto | null>(null)
const periods = ref<AllocationPeriodDto[]>([])
const loading = ref(true)

const maxFacultyRooms = computed(() => {
  if (!stats.value?.allocationsByFaculty.length) return 1
  return Math.max(...stats.value.allocationsByFaculty.map(a => a.roomCount))
})

// --- Student participation state ---
const eligibility = ref<MyEligibilityResult | null>(null)
const participateResult = ref<ParticipateResult | null>(null)
const matricInput = ref('')
const participating = ref(false)
const participateError = ref('')
const activePeriodForStudent = ref<AllocationPeriodDto | null>(null)
const myAllocation = ref<DormAllocationDto | null | undefined>(undefined)
const myGroup = ref<GroupDto | null>(null)
const myRoom = ref<RoomAssignmentDto | null | undefined>(undefined)
const declineDialogOpen = ref(false)
const declineLoading = ref(false)
const acceptLoading = ref(false)

async function loadDashboard() {
  if (isAdmin.value) {
    try {
      const [dashStats, allPeriods] = await Promise.all([
        getDashboardStats(),
        getAllocationPeriods(),
      ])
      stats.value = dashStats
      periods.value = allPeriods
    } catch {
      // silent
    } finally {
      loading.value = false
    }
  } else {
    try {
      const allPeriods = await getAllocationPeriods().catch(() => [] as AllocationPeriodDto[])
      const openPeriod = allPeriods.find(p => p.status === 'Open' || p.status === 'Allocating')
      activePeriodForStudent.value = openPeriod ?? null
      if (openPeriod && authStore.user?.matriculationCode) {
        const [eligResult, allocResult] = await Promise.allSettled([
          getMyEligibility(openPeriod.id),
          getMyAllocation(openPeriod.id),
        ])
        if (eligResult.status === 'fulfilled') eligibility.value = eligResult.value
        if (allocResult.status === 'fulfilled') myAllocation.value = allocResult.value
        if (myAllocation.value?.status === 'Accepted') {
          myGroup.value = await getMyGroup().catch(() => null)
          myRoom.value = await getMyRoom().catch(() => null)
        }
      }
    } catch {
      // silent
    } finally {
      loading.value = false
    }
  }
}

async function handleAccept() {
  if (!myAllocation.value) return
  acceptLoading.value = true
  try {
    await acceptAllocation(myAllocation.value.id)
    toast.success('Allocation accepted.')
    if (activePeriodForStudent.value) {
      myAllocation.value = await getMyAllocation(activePeriodForStudent.value.id)
    }
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not accept the allocation.')
    } else {
      toast.error('Could not accept the allocation.')
    }
  } finally {
    acceptLoading.value = false
  }
}

async function handleDeclineConfirm() {
  if (!myAllocation.value) return
  declineLoading.value = true
  try {
    await declineAllocation(myAllocation.value.id)
    toast.success("Allocation declined. You are no longer in this period's pool.")
    declineDialogOpen.value = false
    if (activePeriodForStudent.value) {
      myAllocation.value = await getMyAllocation(activePeriodForStudent.value.id)
    }
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not decline the allocation.')
    } else {
      toast.error('Could not decline the allocation.')
    }
  } finally {
    declineLoading.value = false
  }
}

async function handleParticipate() {
  if (!activePeriodForStudent.value) return
  const code = matricInput.value.trim() || undefined
  if (!code && !authStore.user?.matriculationCode) {
    participateError.value = 'Please enter your matriculation code.'
    return
  }
  participating.value = true
  participateError.value = ''
  try {
    participateResult.value = await participate(activePeriodForStudent.value.id, { matriculationCode: code })
    await authStore.fetchCurrentUser()
    eligibility.value = await getMyEligibility(activePeriodForStudent.value.id)
    toast.success('You have successfully participated!')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      participateError.value = data.detail ?? 'Failed to participate.'
    }
  } finally {
    participating.value = false
  }
}

// Wait for auth store to finish loading before fetching dashboard data
watch(() => authStore.isLoading, (isLoading) => {
  if (!isLoading && authStore.user) loadDashboard()
}, { immediate: true })

function statusColor(status: string) {
  if (status === 'Open') return 'bg-emerald-500'
  if (status === 'Draft') return 'bg-amber-500'
  return 'bg-muted-foreground/30'
}

function statusVariant(status: string) {
  if (status === 'Open') return 'default' as const
  if (status === 'Draft') return 'outline' as const
  return 'secondary' as const
}

function formatDate(dateStr: string) {
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', year: 'numeric' })
}

function allocStatusVariant(status: string) {
  if (status === 'Accepted') return 'default' as const
  if (status === 'Declined') return 'destructive' as const
  if (status === 'Expired') return 'secondary' as const
  if (status === 'Replaced') return 'secondary' as const
  return 'outline' as const
}

const participationSubtext = computed(() => {
  const status = myAllocation.value?.status
  switch (status) {
    case 'Pending':
      return 'You have been placed for this period. Please respond before the window closes.'
    case 'Accepted':
      return 'You have accepted your placement for this period.'
    case 'Declined':
    case 'Expired':
    case 'Replaced':
      return 'Your participation in this period has ended.'
    default:
      return 'Your allocation will be processed when the period enters the allocating phase.'
  }
})

const isParticipationTerminal = computed(() => {
  const status = myAllocation.value?.status
  return status === 'Declined' || status === 'Expired' || status === 'Replaced'
})

const participationCardClass = computed(() =>
  isParticipationTerminal.value
    ? 'border-muted-foreground/20 bg-muted/40'
    : 'border-emerald-500/30 bg-emerald-500/5'
)

const participationIcon = computed(() =>
  isParticipationTerminal.value ? CircleSlash2 : CheckCircle2
)

const participationIconClass = computed(() =>
  isParticipationTerminal.value ? 'size-5 text-muted-foreground' : 'size-5 text-emerald-600'
)

function placementLocation(dormName: string, campusName: string) {
  return dormName === campusName
    ? dormName
    : `${dormName} in ${campusName}`
}

const allocationCopy = computed(() => {
  const a = myAllocation.value
  if (!a) return ''
  const where = placementLocation(a.dormitoryName, a.campusName)
  switch (a.status) {
    case 'Pending':
      return `You've been placed in ${where}. Respond before the window closes.`
    case 'Accepted':
      return `You've accepted your placement in ${where}.`
    case 'Declined':
      return `You declined your allocation in ${a.dormitoryName}. You are no longer in this period's pool.`
    case 'Expired':
      return `Your allocation in ${a.dormitoryName} expired because you didn't respond in time. You are no longer in this period's pool.`
    case 'Replaced':
      return `Your previous allocation in ${a.dormitoryName} was replaced by a newer one.`
    default:
      return `Your allocation status: ${a.status}.`
  }
})

const myAllocationCardClass = computed(() => {
  const status = myAllocation.value?.status
  if (status === 'Pending') return 'border-amber-500/30 bg-amber-500/5'
  if (status === 'Accepted') return 'border-emerald-500/30 bg-emerald-500/5'
  if (status === 'Declined' || status === 'Expired' || status === 'Replaced')
    return 'border-muted-foreground/20 bg-muted/30'
  return ''
})
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-lg font-semibold tracking-tight">Dashboard</h1>
          <p class="text-xs text-muted-foreground">Allocation overview and key metrics.</p>
        </div>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
        <Skeleton v-for="i in 4" :key="i" class="h-18" />
      </div>

      <template v-else-if="isAdmin && stats">
        <!-- KPI strip: divide-x cells in a single bordered surface (no per-card boxes) -->
        <section
          class="overflow-hidden rounded-lg border bg-card"
          aria-label="Allocation overview"
        >
          <div class="grid grid-cols-2 divide-y divide-x lg:grid-cols-4 lg:divide-y-0">
            <RouterLink
              to="/campuses"
              class="flex items-center gap-3 p-3 transition-colors hover:bg-muted/40 focus-visible:bg-muted/40 focus-visible:outline-none"
            >
              <span class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <Building2 class="size-4" />
              </span>
              <span class="block min-w-0">
                <span class="block text-[10px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">Campuses</span>
                <span class="mt-0.5 block text-xl font-semibold font-mono tabular-nums leading-none">{{ stats.campusCount }}</span>
              </span>
            </RouterLink>

            <RouterLink
              to="/campuses"
              class="flex items-center gap-3 p-3 transition-colors hover:bg-muted/40 focus-visible:bg-muted/40 focus-visible:outline-none"
            >
              <span class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <DoorOpen class="size-4" />
              </span>
              <span class="block min-w-0">
                <span class="block text-[10px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">Dormitories</span>
                <span class="mt-0.5 block text-xl font-semibold font-mono tabular-nums leading-none">{{ stats.dormitoryCount }}</span>
              </span>
            </RouterLink>

            <RouterLink
              to="/campuses"
              class="flex items-center gap-3 p-3 transition-colors hover:bg-muted/40 focus-visible:bg-muted/40 focus-visible:outline-none"
            >
              <span class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <BedDouble class="size-4" />
              </span>
              <span class="block min-w-0">
                <span class="block text-[10px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">Rooms</span>
                <span class="mt-0.5 flex items-baseline gap-1.5">
                  <span class="text-xl font-semibold font-mono tabular-nums leading-none">{{ stats.totalRooms }}</span>
                  <span class="text-[10px] font-mono text-muted-foreground">{{ stats.totalCapacity }} beds</span>
                </span>
              </span>
            </RouterLink>

            <RouterLink
              to="/allocation"
              class="flex items-center gap-3 p-3 transition-colors hover:bg-muted/40 focus-visible:bg-muted/40 focus-visible:outline-none"
            >
              <span
                class="flex size-8 shrink-0 items-center justify-center rounded-md"
                :class="stats.activePeriod ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400' : 'bg-muted text-muted-foreground'"
              >
                <CalendarClock class="size-4" />
              </span>
              <span class="block min-w-0">
                <span class="block text-[10px] font-semibold uppercase tracking-[0.08em] text-muted-foreground">Period</span>
                <span class="mt-0.5 flex items-center gap-1.5 truncate">
                  <span class="truncate text-sm font-medium leading-none">
                    {{ stats.activePeriod?.name ?? 'None active' }}
                  </span>
                  <Badge
                    v-if="stats.activePeriod"
                    :variant="statusVariant(stats.activePeriod.status)"
                    class="h-4 px-1.5 text-[9px]"
                  >
                    {{ stats.activePeriod.status }}
                  </Badge>
                </span>
              </span>
            </RouterLink>
          </div>
        </section>

        <!-- Middle row: Allocations + Timeline -->
        <div class="grid gap-2 lg:grid-cols-7">
          <!-- Faculty Allocation Breakdown -->
          <Card class="lg:col-span-4">
            <CardHeader class="p-3 pb-2">
              <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                Room Allocation by Faculty
              </CardTitle>
            </CardHeader>
            <CardContent class="p-3 pt-0">
              <div v-if="stats.allocationsByFaculty.length === 0" class="flex h-24 items-center justify-center text-xs text-muted-foreground">
                No allocations yet. Distribute rooms from the Room Quotas page.
              </div>
              <div v-else class="space-y-2.5">
                <div v-for="alloc in stats.allocationsByFaculty" :key="alloc.facultyId" class="space-y-1">
                  <div class="flex items-center justify-between text-xs">
                    <div class="flex items-center gap-2">
                      <span class="font-medium">{{ alloc.facultyName }}</span>
                      <span class="font-mono text-[10px] text-muted-foreground">{{ alloc.abbreviation }}</span>
                    </div>
                    <span class="font-mono text-[11px] font-medium tabular-nums">{{ alloc.roomCount }}</span>
                  </div>
                  <div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      class="h-full rounded-full bg-primary transition-all duration-500"
                      :style="{ width: `${(alloc.roomCount / maxFacultyRooms) * 100}%` }"
                    />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>

          <!-- Period Timeline -->
          <Card class="lg:col-span-3">
            <CardHeader class="p-3 pb-2">
              <div class="flex items-center justify-between">
                <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">
                  Allocation Periods
                </CardTitle>
                <Button variant="ghost" size="sm" as-child class="h-6 px-2 text-[11px]">
                  <router-link to="/allocation" class="gap-1">
                    Manage <ArrowRight class="size-3" />
                  </router-link>
                </Button>
              </div>
            </CardHeader>
            <CardContent class="p-3 pt-0">
              <div v-if="periods.length === 0" class="flex h-24 items-center justify-center text-xs text-muted-foreground">
                No periods created yet.
              </div>
              <div v-else class="space-y-0">
                <div
                  v-for="(period, i) in periods"
                  :key="period.id"
                  class="relative flex items-start gap-3 py-2"
                  :class="{ 'border-t border-border/50': i > 0 }"
                >
                  <!-- Status dot with connector line -->
                  <div class="relative flex flex-col items-center">
                    <Circle class="size-2.5 shrink-0" :class="statusColor(period.status)" style="fill: currentColor" />
                  </div>
                  <!-- Content -->
                  <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-2">
                      <span class="text-xs font-medium">{{ period.name }}</span>
                      <Badge :variant="statusVariant(period.status)" class="h-4 px-1.5 text-[9px]">
                        {{ period.status }}
                      </Badge>
                    </div>
                    <p class="font-mono text-[10px] text-muted-foreground">
                      {{ formatDate(period.startDate) }} — {{ formatDate(period.endDate) }}
                    </p>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        <!-- Quick Access -->
        <div class="flex flex-wrap gap-1.5">
          <Button variant="outline" size="sm" as-child class="h-7 text-xs">
            <router-link to="/campuses">Manage campuses</router-link>
          </Button>
          <Button variant="outline" size="sm" as-child class="h-7 text-xs">
            <router-link to="/room-quotas">Room quotas</router-link>
          </Button>
          <Button variant="outline" size="sm" as-child class="h-7 text-xs">
            <router-link to="/students">Students</router-link>
          </Button>
          <Button variant="outline" size="sm" as-child class="h-7 text-xs">
            <router-link to="/faculties">Faculties</router-link>
          </Button>
        </div>
      </template>

      <!-- Student view -->
      <template v-else>
        <ProfileCompletenessBanner />
        <!-- No active period -->
        <Card v-if="!activePeriodForStudent">
          <CardContent class="flex flex-col items-center justify-center py-12">
            <CalendarClock class="mb-3 size-10 text-muted-foreground" />
            <p class="text-sm font-medium">No active allocation period</p>
            <p class="text-xs text-muted-foreground">Check back when a period is open for participation.</p>
          </CardContent>
        </Card>

        <!-- Already participated -->
        <template v-else-if="eligibility?.hasParticipated">
          <Card :class="participationCardClass">
            <CardContent class="p-4">
              <div class="flex items-center gap-3">
                <component :is="participationIcon" :class="participationIconClass" />
                <div>
                  <p class="text-sm font-medium">You are participating in {{ activePeriodForStudent.name }}</p>
                  <p class="text-xs text-muted-foreground">{{ participationSubtext }}</p>
                </div>
              </div>
            </CardContent>
          </Card>
          <Card>
            <CardContent class="p-4">
              <div class="grid gap-3 sm:grid-cols-3">
                <div class="space-y-1">
                  <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Faculty</p>
                  <div class="flex items-center gap-2">
                    <GraduationCap class="size-4 text-primary" />
                    <span class="text-sm font-medium">{{ eligibility.facultyName }}</span>
                    <Badge variant="outline" class="font-mono text-[10px]">{{ eligibility.facultyAbbreviation }}</Badge>
                  </div>
                </div>
                <div class="space-y-1">
                  <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Points</p>
                  <p class="text-xl font-semibold font-mono tabular-nums">{{ eligibility.points }}</p>
                </div>
                <div class="space-y-1">
                  <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Matriculation Code</p>
                  <p class="text-sm font-mono">{{ eligibility.matriculationCode }}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          <!-- My Dormitory Allocation -->
          <Card :class="['transition-colors duration-200', myAllocationCardClass]">
            <CardHeader class="p-3 pb-2">
              <CardTitle class="flex items-center gap-2 text-sm font-medium">
                <Home class="size-4 text-primary" />
                My Dormitory Allocation
              </CardTitle>
            </CardHeader>
            <CardContent class="p-3 pt-0">
              <!-- Loading -->
              <Skeleton v-if="myAllocation === undefined" class="h-12" />

              <!-- Has allocation -->
              <template v-else-if="myAllocation">
                <p class="text-sm">{{ allocationCopy }}</p>
                <div class="mt-2 flex flex-wrap items-center gap-3">
                  <div class="space-y-0.5">
                    <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Status</p>
                    <Badge :variant="allocStatusVariant(myAllocation.status)" class="h-5 px-1.5 text-[10px]">
                      {{ myAllocation.status }}
                    </Badge>
                  </div>
                  <div class="space-y-0.5">
                    <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Round</p>
                    <p class="font-mono text-sm font-medium">{{ myAllocation.round }}</p>
                  </div>
                  <div class="space-y-0.5">
                    <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Allocated</p>
                    <p class="font-mono text-xs text-muted-foreground">
                      {{ new Date(myAllocation.allocatedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' }) }}
                    </p>
                  </div>
                </div>
                <div
                  v-if="myAllocation.status === 'Pending'"
                  class="mt-3 flex flex-wrap items-center gap-2"
                >
                  <Button
                    size="sm"
                    class="h-7 text-xs transition-transform active:scale-[0.98]"
                    :disabled="acceptLoading || declineLoading"
                    @click="handleAccept"
                  >
                    <Loader2 v-if="acceptLoading" class="mr-1.5 size-3 animate-spin" />
                    Accept
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    class="h-7 text-xs transition-transform active:scale-[0.98]"
                    :disabled="acceptLoading || declineLoading"
                    @click="declineDialogOpen = true"
                  >
                    Decline
                  </Button>
                </div>
                <p
                  v-if="myAllocation.status === 'Declined' || myAllocation.status === 'Expired'"
                  class="mt-2 text-xs text-muted-foreground"
                >
                  Contact your faculty admin if this is wrong.
                </p>
                <!-- Next-step panel (only shown when room not yet assigned) -->
                <div v-if="myAllocation.status === 'Accepted' && !myRoom" class="mt-4 space-y-3">
                  <!-- No group: two clear paths -->
                  <section
                    v-if="!myGroup"
                    class="relative overflow-hidden rounded-xl border border-primary/15 bg-gradient-to-br from-primary/[0.05] via-primary/[0.02] to-transparent p-4"
                  >
                    <div
                      class="pointer-events-none absolute -right-12 -top-12 size-48 rounded-full opacity-50"
                      style="background: radial-gradient(circle, oklch(0.5 0.18 259 / 0.18) 0%, transparent 70%)"
                      aria-hidden="true"
                    />

                    <div class="relative">
                      <p class="text-[10px] font-semibold uppercase tracking-[0.18em] text-primary/70">
                        Next step
                      </p>
                      <h3 class="mt-0.5 text-sm font-semibold tracking-tight">
                        Choose how you want to be placed
                      </h3>
                    </div>

                    <div class="relative mt-4 grid gap-3 sm:grid-cols-2">
                      <!-- Form a group -->
                      <article class="flex flex-col gap-3 rounded-lg border border-border/60 bg-background/80 p-4">
                        <header class="flex items-start gap-3">
                          <span class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 ring-1 ring-primary/15">
                            <Users2 class="size-4 text-primary" />
                          </span>
                          <div class="min-w-0">
                            <h4 class="text-sm font-semibold tracking-tight">Form a group</h4>
                            <p class="mt-0.5 text-xs leading-relaxed text-muted-foreground">
                              Invite up to two classmates and pick a room together.
                            </p>
                          </div>
                        </header>
                        <div class="mt-auto flex justify-end">
                          <Button size="sm" as-child class="group">
                            <router-link to="/lobby">
                              Open lobby
                              <ArrowRight class="ml-1 size-3.5 transition-transform group-hover:translate-x-0.5" />
                            </router-link>
                          </Button>
                        </div>
                      </article>

                      <!-- Place me solo -->
                      <article class="flex flex-col gap-3 rounded-lg border border-border/60 bg-background/80 p-4">
                        <header class="flex items-start gap-3">
                          <span class="flex size-9 shrink-0 items-center justify-center rounded-lg bg-amber-500/10 ring-1 ring-amber-500/20">
                            <BedDouble class="size-4 text-amber-600 dark:text-amber-400" />
                          </span>
                          <div class="min-w-0">
                            <h4 class="text-sm font-semibold tracking-tight">Go solo</h4>
                            <p class="mt-0.5 text-xs leading-relaxed text-muted-foreground">
                              Skip the group and we'll assign you the best available room.
                            </p>
                          </div>
                        </header>
                        <div class="mt-auto flex justify-end">
                          <PlaceMeNowButton group-status="no-group" display="inline" @placed="loadDashboard" />
                        </div>
                      </article>
                    </div>
                  </section>

                  <!-- Forming group: progress + open lobby -->
                  <section
                    v-else-if="myGroup.status === 'Forming'"
                    class="rounded-xl border border-amber-500/25 bg-amber-500/[0.04] p-4"
                  >
                    <div class="flex items-start justify-between gap-3">
                      <div>
                        <p class="text-[10px] font-semibold uppercase tracking-[0.18em] text-amber-700/80 dark:text-amber-400/80">
                          Group forming
                        </p>
                        <p class="mt-1 text-sm">
                          <span class="font-mono tabular-nums font-semibold">{{ myGroup.members.length }}</span>
                          of
                          <span class="font-mono tabular-nums">{{ myGroup.roomSizePreference }}</span>
                          members joined
                        </p>
                      </div>
                      <Button variant="outline" size="sm" class="h-7 text-xs shrink-0" as-child>
                        <router-link to="/lobby">Open lobby</router-link>
                      </Button>
                    </div>
                  </section>

                  <!-- Locked group: waiting for placement -->
                  <section
                    v-else-if="myGroup.status === 'Locked'"
                    class="rounded-xl border border-emerald-500/25 bg-emerald-500/[0.05] p-4"
                  >
                    <p class="text-[10px] font-semibold uppercase tracking-[0.18em] text-emerald-700/80 dark:text-emerald-400/80">
                      Group locked
                    </p>
                    <p class="mt-1 text-sm">
                      You're rooming with
                      <span class="font-medium">{{ myGroup.members.filter(m => m.userId !== authStore.user?.id).map(m => m.firstName).join(', ') }}</span>.
                      Your room will appear here shortly.
                    </p>
                  </section>
                </div>

                <!-- Room assigned: milestone card -->
                <div v-if="myRoom" class="mt-4">
                  <RoomCard :assignment="myRoom" />
                </div>
              </template>

              <!-- No allocation yet -->
              <template v-else>
                <p class="text-sm text-muted-foreground">You don't have a dorm allocation yet for this period.</p>
                <Button variant="outline" size="sm" class="mt-2 h-7 text-xs" as-child>
                  <router-link to="/preferences">View my preferences</router-link>
                </Button>
              </template>
            </CardContent>
          </Card>
        </template>

        <!-- Can participate -->
        <template v-else>
          <Card>
            <CardContent class="p-4">
              <div class="flex items-center gap-3 mb-4">
                <div class="flex size-2.5 rounded-full bg-emerald-500" />
                <div>
                  <p class="text-sm font-medium">{{ activePeriodForStudent.name }} is open</p>
                  <p class="text-xs text-muted-foreground">Participate to be included in the dormitory allocation.</p>
                </div>
              </div>

              <!-- Matriculation code input (if not set) -->
              <div v-if="!authStore.user?.matriculationCode" class="mb-4 space-y-2">
                <Label for="matric-participate" class="text-xs">Enter your matriculation code to participate</Label>
                <div class="flex gap-2">
                  <Input
                    id="matric-participate"
                    v-model="matricInput"
                    placeholder="e.g. CS2024001"
                    class="w-48 font-mono"
                    @keydown.enter="handleParticipate"
                  />
                </div>
              </div>

              <!-- Already has code -->
              <p v-else class="mb-4 text-xs text-muted-foreground">
                Using matriculation code: <span class="font-mono font-medium text-foreground">{{ authStore.user.matriculationCode }}</span>
              </p>

              <p v-if="participateError" role="alert" class="mb-3 text-xs text-destructive">{{ participateError }}</p>

              <Button :disabled="participating" @click="handleParticipate">
                {{ participating ? 'Participating...' : 'Participate' }}
              </Button>
            </CardContent>
          </Card>
        </template>
      </template>
    </div>

    <DeclineAllocationDialog
      v-if="myAllocation"
      v-model:open="declineDialogOpen"
      :dormitory-name="myAllocation.dormitoryName"
      :loading="declineLoading"
      @confirm="handleDeclineConfirm"
    />
  </AppLayout>
</template>