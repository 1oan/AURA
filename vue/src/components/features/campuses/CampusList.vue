<script setup lang="ts">
import { Building2, Pencil, Trash2, Plus, MapPin } from 'lucide-vue-next'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import type { CampusDto } from '@/api/campuses'

defineProps<{
  campuses: CampusDto[]
  loading: boolean
}>()

const emit = defineEmits<{
  select: [campusId: string]
  edit: [campus: CampusDto]
  delete: [campus: CampusDto]
  create: []
}>()
</script>

<template>
  <!-- Loading -->
  <div v-if="loading" class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
    <Skeleton v-for="i in 6" :key="i" class="h-20" />
  </div>

  <!-- Empty state -->
  <div
    v-else-if="campuses.length === 0"
    class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
  >
    <Building2 class="mb-3 size-10 text-muted-foreground" />
    <p class="text-sm font-medium">No campuses yet</p>
    <p class="mb-4 text-xs text-muted-foreground">Create your first campus to get started.</p>
    <Button size="sm" @click="emit('create')">
      <Plus class="mr-1.5 size-3.5" />
      Add Campus
    </Button>
  </div>

  <!-- Campus grid -->
  <div v-else class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
    <Card
      v-for="campus in campuses"
      :key="campus.id"
      class="group cursor-pointer transition-colors hover:bg-muted/30"
      @click="emit('select', campus.id)"
    >
      <CardContent class="flex items-center gap-3 p-3">
        <div class="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
          <Building2 class="size-4" />
        </div>
        <div class="min-w-0 flex-1">
          <p class="text-sm font-medium leading-tight">{{ campus.name }}</p>
          <p v-if="campus.address" class="flex items-center gap-1 truncate text-xs text-muted-foreground">
            <MapPin class="size-3 shrink-0" />
            {{ campus.address }}
          </p>
        </div>
        <div class="flex items-center gap-0.5 opacity-100 md:opacity-0 md:group-hover:opacity-100 transition-opacity">
          <Button
            variant="ghost"
            size="icon"
            class="size-7"
            @click.stop="emit('edit', campus)"
          >
            <Pencil class="size-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            class="size-7"
            @click.stop="emit('delete', campus)"
          >
            <Trash2 class="size-3.5" />
          </Button>
        </div>
      </CardContent>
    </Card>
  </div>
</template>
