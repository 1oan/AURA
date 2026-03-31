<script setup lang="ts">
import { Button } from '@/components/ui/button'
import {
  AlertDialog,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'

interface Props {
  open: boolean
  targetName: string
  error: string
  loading: boolean
}

defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  confirmed: []
}>()
</script>

<template>
  <AlertDialog :open="open" @update:open="emit('update:open', $event)">
    <AlertDialogContent>
      <AlertDialogHeader>
        <AlertDialogTitle>Are you sure?</AlertDialogTitle>
        <AlertDialogDescription>
          This will permanently delete <strong>{{ targetName }}</strong>.
          This action cannot be undone.
        </AlertDialogDescription>
      </AlertDialogHeader>
      <p v-if="error" role="alert" class="text-sm text-destructive">{{ error }}</p>
      <AlertDialogFooter>
        <AlertDialogCancel :disabled="loading">Cancel</AlertDialogCancel>
        <Button
          variant="destructive"
          :disabled="loading"
          @click="emit('confirmed')"
        >
          {{ loading ? 'Deleting...' : 'Delete' }}
        </Button>
      </AlertDialogFooter>
    </AlertDialogContent>
  </AlertDialog>
</template>
