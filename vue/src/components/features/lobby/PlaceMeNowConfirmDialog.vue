<script setup lang="ts">
import { computed } from 'vue'
import { BedDouble, Info, Sparkles, Lock, AlertCircle } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'

interface Props {
  open: boolean
  variant: 'no-group' | 'leader' | 'member'
}

const props = defineProps<Props>()
const emit = defineEmits<{
  'update:open': [value: boolean]
  confirm: []
  cancel: []
}>()

const isConfirmable = computed(() => props.variant === 'no-group')

const title = computed(() => {
  if (props.variant === 'no-group') return 'Place me in a room?'
  return 'Solo placement unavailable'
})

const bullets = computed(() => {
  if (props.variant === 'no-group') {
    return [
      { icon: Sparkles, text: 'We pick the best available room in your dormitory.' },
      { icon: Lock, text: "Once assigned, the room can't be changed." },
      { icon: AlertCircle, text: "You won't be able to join a roommate group after this." },
    ]
  }
  if (props.variant === 'leader') {
    return [
      { icon: Info, text: 'You lead a forming group — lock it once full, or disband it first.' },
    ]
  }
  return [
    { icon: Info, text: 'You are part of a forming group — ask your leader to lock it, or leave the group first.' },
  ]
})

function onCancel() {
  emit('update:open', false)
  emit('cancel')
}

function onConfirm() {
  emit('confirm')
}
</script>

<template>
  <Dialog :open="props.open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-md">
      <DialogHeader class="items-start gap-3">
        <span
          class="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 ring-1 ring-primary/20"
          aria-hidden="true"
        >
          <BedDouble v-if="isConfirmable" class="size-5 text-primary" />
          <Info v-else class="size-5 text-primary" />
        </span>
        <DialogTitle class="text-balance text-xl font-semibold tracking-tight leading-tight">
          {{ title }}
        </DialogTitle>
        <DialogDescription class="sr-only">
          {{ title }}
        </DialogDescription>
      </DialogHeader>

      <ul class="space-y-2.5 py-1">
        <li
          v-for="(b, i) in bullets"
          :key="i"
          class="flex items-start gap-2.5"
        >
          <component :is="b.icon" class="mt-0.5 size-4 shrink-0 text-muted-foreground" aria-hidden="true" />
          <span class="text-[13px] leading-relaxed text-foreground/80">{{ b.text }}</span>
        </li>
      </ul>

      <DialogFooter class="mt-2 gap-2 sm:gap-2">
        <Button
          v-if="isConfirmable"
          type="button"
          variant="ghost"
          size="sm"
          @click="onCancel"
        >
          Not yet
        </Button>
        <Button
          v-if="isConfirmable"
          type="button"
          size="sm"
          class="transition-transform active:scale-[0.98]"
          @click="onConfirm"
        >
          Yes, place me
        </Button>
        <Button
          v-else
          type="button"
          variant="outline"
          size="sm"
          class="w-full sm:w-auto"
          @click="onCancel"
        >
          Got it
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
