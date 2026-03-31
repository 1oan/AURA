<script setup lang="ts">
import { ref, watch } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { ApiError } from '@/api/client'
import { createDormitory, updateDormitory } from '@/api/dormitories'
import type { DormitoryDto } from '@/api/dormitories'

interface Props {
  open: boolean
  mode: 'create' | 'edit'
  dormitory: DormitoryDto | null
  campusId: string
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: [dormitory: DormitoryDto]
}>()

const name = ref('')
const error = ref('')
const saving = ref(false)

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      name.value = props.dormitory?.name ?? ''
      error.value = ''
    }
  },
)

async function save() {
  error.value = ''
  if (!name.value.trim()) {
    error.value = 'Name is required.'
    return
  }
  saving.value = true
  try {
    if (props.mode === 'create') {
      const created = await createDormitory({
        name: name.value.trim(),
        campusId: props.campusId,
      })
      toast.success('Dormitory created successfully.')
      emit('saved', created)
    } else {
      await updateDormitory(props.dormitory!.id, { name: name.value.trim() })
      toast.success('Dormitory updated successfully.')
      emit('saved', {
        id: props.dormitory!.id,
        name: name.value.trim(),
        campusId: props.dormitory!.campusId,
      })
    }
    emit('update:open', false)
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'An unexpected error occurred.'
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-md">
      <DialogHeader>
        <DialogTitle>{{ mode === 'create' ? 'Add Dormitory' : 'Edit Dormitory' }}</DialogTitle>
        <DialogDescription>
          {{ mode === 'create' ? 'Create a new dormitory.' : 'Update dormitory details.' }}
        </DialogDescription>
      </DialogHeader>
      <form class="space-y-4" @submit.prevent="save">
        <div class="space-y-2">
          <Label for="dorm-name">Name</Label>
          <Input id="dorm-name" v-model="name" placeholder="e.g. C10" required />
        </div>
        <p v-if="error" class="text-sm text-destructive">{{ error }}</p>
        <DialogFooter>
          <Button type="submit" :disabled="saving">
            {{ saving ? 'Saving...' : 'Save' }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
