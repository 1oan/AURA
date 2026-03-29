<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Building2, Users, DoorOpen, CheckCircle2, ArrowRight } from 'lucide-vue-next'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { useAuthStore } from '@/stores/auth'
import { getCampuses } from '@/api/campuses'
import { getFaculties } from '@/api/faculties'
import { getAllocationPeriods } from '@/api/allocationPeriods'

const authStore = useAuthStore()
const isAdmin = computed(() => ['SuperAdmin', 'FacultyAdmin'].includes(authStore.user?.role ?? ''))

const totalCampuses = ref(0)
const totalFaculties = ref(0)
const activePeriod = ref<string | null>(null)
const loading = ref(true)

onMounted(async () => {
  if (!isAdmin.value) {
    loading.value = false
    return
  }
  try {
    const [campuses, faculties, periods] = await Promise.all([
      getCampuses(),
      getFaculties(),
      getAllocationPeriods(),
    ])
    totalCampuses.value = campuses.length
    totalFaculties.value = faculties.length
    const active = periods.find(p => p.status === 'Open')
    activePeriod.value = active?.name ?? null
  } catch {
    // silent — dashboard is informational
  } finally {
    loading.value = false
  }
})

const stats = [
  { title: 'Campuses', value: totalCampuses, icon: Building2, href: '/campuses' },
  { title: 'Faculties', value: totalFaculties, icon: DoorOpen, href: '/faculties' },
  { title: 'Students', value: 0, icon: Users, href: '/students' },
  { title: 'Confirmed', value: 0, icon: CheckCircle2, href: '/confirmations' },
]
</script>

<template>
  <AppLayout>
    <div class="space-y-6">
      <div>
        <h1 class="text-xl font-semibold tracking-tight">Dashboard</h1>
        <p class="mt-0.5 text-sm text-muted-foreground">
          Overview of allocation status and key metrics.
        </p>
      </div>

      <!-- Stats (admin only) -->
      <div v-if="isAdmin" class="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <Card v-for="stat in stats" :key="stat.title">
          <CardContent class="flex items-center gap-4 p-4">
            <div class="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
              <component :is="stat.icon" class="size-4" />
            </div>
            <div class="min-w-0">
              <p class="text-[11px] font-medium uppercase tracking-wider text-muted-foreground">{{ stat.title }}</p>
              <p class="text-lg font-semibold tabular-nums leading-tight">
                {{ loading ? '...' : stat.value }}
              </p>
            </div>
          </CardContent>
        </Card>
      </div>

      <!-- Active period banner (admin only) -->
      <Card v-if="isAdmin && !loading" class="border-primary/20 bg-primary/5">
        <CardContent class="flex items-center justify-between p-4">
          <div class="flex items-center gap-3">
            <div class="flex size-2 rounded-full" :class="activePeriod ? 'bg-emerald-500' : 'bg-muted-foreground/30'" />
            <div>
              <p class="text-sm font-medium">
                {{ activePeriod ? `Active period: ${activePeriod}` : 'No active allocation period' }}
              </p>
              <p class="text-xs text-muted-foreground">
                {{ activePeriod ? 'Students can submit preferences' : 'Activate a period to start the allocation process' }}
              </p>
            </div>
          </div>
          <Button variant="ghost" size="sm" as-child>
            <router-link to="/allocation" class="gap-1.5">
              Manage
              <ArrowRight class="size-3.5" />
            </router-link>
          </Button>
        </CardContent>
      </Card>

      <!-- Quick actions + Activity (admin only) -->
      <div v-if="isAdmin" class="grid gap-3 lg:grid-cols-7">
        <Card class="lg:col-span-4">
          <CardHeader class="pb-3">
            <CardTitle class="text-sm font-medium">Allocation Progress</CardTitle>
          </CardHeader>
          <CardContent>
            <div class="flex h-40 items-center justify-center text-sm text-muted-foreground">
              Chart will appear once allocation data is available.
            </div>
          </CardContent>
        </Card>
        <Card class="lg:col-span-3">
          <CardHeader class="pb-3">
            <CardTitle class="text-sm font-medium">Quick Actions</CardTitle>
          </CardHeader>
          <CardContent class="space-y-1.5">
            <Button variant="ghost" size="sm" as-child class="w-full justify-start gap-2 text-[13px]">
              <router-link to="/campuses">
                <Building2 class="size-3.5 text-muted-foreground" />
                Manage campuses & rooms
              </router-link>
            </Button>
            <Button variant="ghost" size="sm" as-child class="w-full justify-start gap-2 text-[13px]">
              <router-link to="/room-quotas">
                <DoorOpen class="size-3.5 text-muted-foreground" />
                Distribute room quotas
              </router-link>
            </Button>
            <Button variant="ghost" size="sm" as-child class="w-full justify-start gap-2 text-[13px]">
              <router-link to="/students">
                <Users class="size-3.5 text-muted-foreground" />
                Upload student data
              </router-link>
            </Button>
            <Button variant="ghost" size="sm" as-child class="w-full justify-start gap-2 text-[13px]">
              <router-link to="/allocation">
                <CheckCircle2 class="size-3.5 text-muted-foreground" />
                Configure allocation period
              </router-link>
            </Button>
          </CardContent>
        </Card>
      </div>
    </div>
  </AppLayout>
</template>