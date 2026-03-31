<script setup lang="ts">
import { DoorOpen, Pencil, Trash2, Plus } from 'lucide-vue-next'
import { Card, CardContent } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import type { DormitoryDto } from '@/api/dormitories'

defineProps<{
  dormitories: DormitoryDto[]
  loading: boolean
}>()

const emit = defineEmits<{
  select: [dormitoryId: string]
  edit: [dormitory: DormitoryDto]
  delete: [dormitory: DormitoryDto]
  create: []
}>()
</script>

<template>
  <!-- Loading -->
  <div v-if="loading" class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
    <Skeleton v-for="i in 6" :key="i" class="h-16" />
  </div>

  <!-- Empty state -->
  <div
    v-else-if="dormitories.length === 0"
    class="flex flex-col items-center justify-center rounded-lg border border-dashed py-16"
  >
    <DoorOpen class="mb-3 size-10 text-muted-foreground" />
    <p class="text-sm font-medium">No dormitories</p>
    <p class="mb-4 text-xs text-muted-foreground">Add a dormitory to this campus.</p>
    <Button size="sm" @click="emit('create')">
      <Plus class="mr-1.5 size-3.5" />
      Add Dormitory
    </Button>
  </div>

  <!-- Dormitory grid -->
  <div v-else class="grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
    <Card
      v-for="dorm in dormitories"
      :key="dorm.id"
      class="group cursor-pointer transition-colors hover:bg-muted/30"
      @click="emit('select', dorm.id)"
    >
      <CardContent class="flex items-center gap-3 p-3">
        <div class="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary/8 text-primary">
          <DoorOpen class="size-4" />
        </div>
        <div class="min-w-0 flex-1">
          <p class="text-sm font-medium leading-tight">{{ dorm.name }}</p>
        </div>
        <div class="flex items-center gap-0.5 opacity-100 md:opacity-0 md:group-hover:opacity-100 transition-opacity">
          <Button
            variant="ghost"
            size="icon"
            class="size-7"
            @click.stop="emit('edit', dorm)"
          >
            <Pencil class="size-3.5" />
          </Button>
          <Button
            variant="ghost"
            size="icon"
            class="size-7"
            @click.stop="emit('delete', dorm)"
          >
            <Trash2 class="size-3.5" />
          </Button>
        </div>
      </CardContent>
    </Card>
  </div>
</template>
