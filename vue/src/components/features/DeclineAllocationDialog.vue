<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { AlertTriangle, Check, Loader2 } from 'lucide-vue-next'

interface Props {
  open: boolean
  dormitoryName: string
  loading?: boolean
}

const props = withDefaults(defineProps<Props>(), { loading: false })

const emit = defineEmits<{
  'update:open': [value: boolean]
  confirm: []
}>()

const typed = ref('')

watch(() => props.open, (isOpen) => {
  if (isOpen) typed.value = ''
})

const matches = computed(() => typed.value.trim() === props.dormitoryName.trim())
const hasTyped = computed(() => typed.value.length > 0)
const showMismatch = computed(() => hasTyped.value && !matches.value)

function onConfirm() {
  if (!matches.value || props.loading) return
  emit('confirm')
}
</script>

<template>
  <Dialog :open="open" @update:open="emit('update:open', $event)">
    <DialogContent class="sm:max-w-md">
      <DialogHeader class="items-start gap-4">
        <span
          class="flex size-10 shrink-0 items-center justify-center rounded-full bg-destructive/10 ring-1 ring-destructive/20"
          aria-hidden="true"
        >
          <AlertTriangle class="size-5 text-destructive" />
        </span>
        <DialogTitle class="text-balance text-lg font-semibold tracking-tight leading-tight">
          Decline this allocation?
        </DialogTitle>
      </DialogHeader>

      <div class="space-y-3">
        <p class="text-sm leading-relaxed text-muted-foreground">
          You are about to decline your placement in
          <strong class="font-semibold text-foreground">{{ dormitoryName }}</strong>.
        </p>

        <div class="rounded-md border-l-2 border-destructive/60 bg-destructive/5 px-3 py-2.5">
          <p class="text-xs font-medium text-destructive">
            This action is irreversible.
          </p>
          <p class="mt-0.5 text-xs leading-relaxed text-destructive/85">
            You will be removed from this period's allocation pool and cannot rejoin
            or be placed in a different dorm.
          </p>
        </div>

        <p class="text-xs text-muted-foreground">
          Contact your faculty admin if you change your mind.
        </p>
      </div>

      <div class="space-y-1.5 pt-1">
        <Label for="decline-confirm" class="text-xs">
          Type the dorm name to confirm
        </Label>
        <div class="relative">
          <Input
            id="decline-confirm"
            v-model="typed"
            :placeholder="dormitoryName"
            autocomplete="off"
            autocapitalize="off"
            spellcheck="false"
            :class="[
              'pr-9',
              showMismatch && 'border-destructive focus-visible:ring-destructive/30',
              matches && 'border-emerald-500/60 focus-visible:ring-emerald-500/20',
            ]"
          />
          <Transition
            enter-active-class="transition duration-150 ease-out"
            enter-from-class="scale-90 opacity-0"
            enter-to-class="scale-100 opacity-100"
          >
            <span
              v-if="matches"
              class="absolute right-2.5 top-1/2 -translate-y-1/2 text-emerald-500"
              aria-hidden="true"
            >
              <Check class="size-4" />
            </span>
          </Transition>
        </div>
        <p
          v-if="showMismatch"
          class="text-xs text-destructive"
          role="alert"
        >
          That doesn't match. Type the dorm name exactly to enable decline.
        </p>
      </div>

      <DialogFooter class="gap-2 sm:gap-0">
        <Button
          type="button"
          variant="outline"
          size="sm"
          :disabled="loading"
          @click="emit('update:open', false)"
        >
          Cancel
        </Button>
        <Button
          type="button"
          variant="destructive"
          size="sm"
          class="transition-transform active:scale-[0.98]"
          :disabled="!matches || loading"
          @click="onConfirm"
        >
          <Loader2 v-if="loading" class="mr-2 size-4 animate-spin" />
          Decline allocation
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
