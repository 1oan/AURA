<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import {
  Building2,
  DoorOpen,
  BedDouble,
  CalendarClock,
  ArrowRight,
  Circle,
} from 'lucide-vue-next'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Skeleton } from '@/components/ui/skeleton'
import { useAuthStore } from '@/stores/auth'
import { getDashboardStats } from '@/api/dashboard'
import type { DashboardStatsDto } from '@/api/dashboard'
import { getAllocationPeriods } from '@/api/allocationPeriods'
import type { AllocationPeriodDto } from '@/api/allocationPeriods'

const authStore = useAuthStore()
const isAdmin = computed(() => ['SuperAdmin', 'FacultyAdmin'].includes(authStore.user?.role ?? ''))

const stats = ref<DashboardStatsDto | null>(null)
const periods = ref<AllocationPeriodDto[]>([])
const loading = ref(true)

const maxFacultyRooms = computed(() => {
  if (!stats.value?.allocationsByFaculty.length) return 1
  return Math.max(...stats.value.allocationsByFaculty.map(a => a.roomCount))
})

async function loadDashboard() {
  if (!isAdmin.value) {
    loading.value = false
    return
  }
  try {
    const [dashStats, allPeriods] = await Promise.all([
      getDashboardStats(),
      getAllocationPeriods(),
    ])
    stats.value = dashStats
    periods.value = allPeriods
  } catch {
    // silent — dashboard is informational
  } finally {
    loading.value = false
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
        <!-- KPI Cards -->
        <div class="grid gap-2 sm:grid-cols-2 lg:grid-cols-4">
          <Card class="cursor-pointer transition-colors hover:bg-muted/30" @click="$router.push('/campuses')">
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <Building2 class="size-4" />
              </div>
              <div>
                <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Campuses</p>
                <p class="text-xl font-semibold font-mono tabular-nums leading-tight">{{ stats.campusCount }}</p>
              </div>
            </CardContent>
          </Card>

          <Card class="cursor-pointer transition-colors hover:bg-muted/30" @click="$router.push('/campuses')">
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <DoorOpen class="size-4" />
              </div>
              <div>
                <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Dormitories</p>
                <p class="text-xl font-semibold font-mono tabular-nums leading-tight">{{ stats.dormitoryCount }}</p>
              </div>
            </CardContent>
          </Card>

          <Card class="cursor-pointer transition-colors hover:bg-muted/30" @click="$router.push('/campuses')">
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
                <BedDouble class="size-4" />
              </div>
              <div>
                <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Rooms</p>
                <p class="text-xl font-semibold font-mono tabular-nums leading-tight">{{ stats.totalRooms }}</p>
                <p class="text-[10px] text-muted-foreground font-mono">{{ stats.totalCapacity }} beds</p>
              </div>
            </CardContent>
          </Card>

          <Card class="cursor-pointer transition-colors hover:bg-muted/30" @click="$router.push('/allocation')">
            <CardContent class="flex items-center gap-3 p-3">
              <div class="flex size-8 shrink-0 items-center justify-center rounded-md" :class="stats.activePeriod ? 'bg-emerald-500/10 text-emerald-600' : 'bg-muted text-muted-foreground'">
                <CalendarClock class="size-4" />
              </div>
              <div>
                <p class="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground">Period</p>
                <p class="text-sm font-medium leading-tight">
                  {{ stats.activePeriod?.name ?? 'None active' }}
                </p>
                <Badge v-if="stats.activePeriod" :variant="statusVariant(stats.activePeriod.status)" class="mt-0.5 h-4 px-1.5 text-[9px]">
                  {{ stats.activePeriod.status }}
                </Badge>
              </div>
            </CardContent>
          </Card>
        </div>

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

      <!-- Non-admin view -->
      <template v-else>
        <Card>
          <CardContent class="flex flex-col items-center justify-center py-12">
            <CalendarClock class="mb-3 size-10 text-muted-foreground" />
            <p class="text-sm font-medium">Welcome to AURA</p>
            <p class="text-xs text-muted-foreground">Check back when an allocation period is open to submit your preferences.</p>
          </CardContent>
        </Card>
      </template>
    </div>
  </AppLayout>
</template>