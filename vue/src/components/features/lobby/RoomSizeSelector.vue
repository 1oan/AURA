<script setup lang="ts">
import { Button } from '@/components/ui/button'

interface Props {
  modelValue: 2 | 3
  minMembers?: number
}

const props = withDefaults(defineProps<Props>(), { minMembers: 1 })
defineEmits<{ 'update:modelValue': [value: 2 | 3] }>()

function canSelect(value: 2 | 3): boolean {
  return value >= props.minMembers
}
</script>

<template>
  <div class="inline-flex gap-1.5">
    <Button
      type="button"
      size="sm"
      :variant="modelValue === 2 ? 'default' : 'outline'"
      :disabled="!canSelect(2)"
      class="h-8"
      @click="$emit('update:modelValue', 2)"
    >
      2-bed
    </Button>
    <Button
      type="button"
      size="sm"
      :variant="modelValue === 3 ? 'default' : 'outline'"
      :disabled="!canSelect(3)"
      class="h-8"
      @click="$emit('update:modelValue', 3)"
    >
      3-bed
    </Button>
  </div>
</template>
