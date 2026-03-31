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
import { createCampus, updateCampus } from '@/api/campuses'
import type { CampusDto } from '@/api/campuses'

interface Props {
  open: boolean
  mode: 'create' | 'edit'
  campus: CampusDto | null
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  saved: [campus: CampusDto]
}>()

const form = ref({ name: '', address: '' })
const error = ref('')
const saving = ref(false)

watch(
  () => props.open,
  (isOpen) => {
    if (isOpen) {
      form.value = {
        name: props.campus?.name ?? '',
        address: props.campus?.address ?? '',
      }
      error.value = ''
    }
  },
)

async function save() {
  error.value = ''
  if (!form.value.name.trim()) {
    error.value = 'Name is required.'
    return
  }
  saving.value = true
  try {
    const payload = {
      name: form.value.name.trim(),
      address: form.value.address.trim() || undefined,
    }
    if (props.mode === 'create') {
      const created = await createCampus(payload)
      toast.success('Campus created successfully.')
      emit('saved', created)
    } else {
      await updateCampus(props.campus!.id, payload)
      toast.success('Campus updated successfully.')
      emit('saved', {
        id: props.campus!.id,
        name: payload.name,
        address: payload.address ?? null,
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
        <DialogTitle>{{ mode === 'create' ? 'Add Campus' : 'Edit Campus' }}</DialogTitle>
        <DialogDescription>
          {{ mode === 'create' ? 'Create a new campus.' : 'Update campus details.' }}
        </DialogDescription>
      </DialogHeader>
      <form class="space-y-4" @submit.prevent="save">
        <div class="space-y-2">
          <Label for="campus-name">Name</Label>
          <Input id="campus-name" v-model="form.name" placeholder="e.g. Codrescu" required />
        </div>
        <div class="space-y-2">
          <Label for="campus-address">Address</Label>
          <Input id="campus-address" v-model="form.address" placeholder="e.g. Str. Codrescu Nr. 1" />
        </div>
        <p v-if="error" role="alert" class="text-sm text-destructive">{{ error }}</p>
        <DialogFooter>
          <Button type="submit" :disabled="saving">
            {{ saving ? 'Saving...' : 'Save' }}
          </Button>
        </DialogFooter>
      </form>
    </DialogContent>
  </Dialog>
</template>
