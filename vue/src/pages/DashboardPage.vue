<script setup lang="ts">
import { Building2, Users, DoorOpen, CheckCircle2 } from 'lucide-vue-next'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

const stats = [
  { title: 'Total Rooms', value: '—', description: 'Across all campuses', icon: Building2 },
  { title: 'Students', value: '—', description: 'Registered this year', icon: Users },
  { title: 'Allocated', value: '—', description: 'Rooms assigned', icon: DoorOpen },
  { title: 'Confirmed', value: '—', description: 'Check-ins completed', icon: CheckCircle2 },
]
</script>

<template>
  <AppLayout>
    <div class="space-y-8">
      <div>
        <h1 class="text-2xl font-semibold tracking-tight">Dashboard</h1>
        <p class="mt-1 text-sm text-muted-foreground">
          Overview of allocation status and key metrics.
        </p>
      </div>

      <div class="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card v-for="stat in stats" :key="stat.title" class="relative overflow-hidden">
          <CardHeader class="flex flex-row items-center justify-between pb-2">
            <CardTitle class="text-sm font-medium text-muted-foreground">
              {{ stat.title }}
            </CardTitle>
            <component
              :is="stat.icon"
              class="size-4 text-muted-foreground/50"
            />
          </CardHeader>
          <CardContent>
            <div class="text-2xl font-semibold">{{ stat.value }}</div>
            <p class="mt-1 text-xs text-muted-foreground">{{ stat.description }}</p>
          </CardContent>
        </Card>
      </div>

      <div class="grid gap-4 lg:grid-cols-7">
        <Card class="lg:col-span-4">
          <CardHeader>
            <CardTitle class="text-sm font-medium">Allocation Progress</CardTitle>
          </CardHeader>
          <CardContent>
            <div class="flex h-48 items-center justify-center text-sm text-muted-foreground">
              Chart will appear once allocation data is available.
            </div>
          </CardContent>
        </Card>
        <Card class="lg:col-span-3">
          <CardHeader>
            <CardTitle class="text-sm font-medium">Recent Activity</CardTitle>
          </CardHeader>
          <CardContent>
            <div class="flex h-48 items-center justify-center text-sm text-muted-foreground">
              No activity yet.
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  </AppLayout>
</template>
