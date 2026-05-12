<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { toast } from 'vue-sonner'
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
import { ApiError } from '@/api/client'
import { disbandGroup } from '@/api/groups'

interface Props {
  open: boolean
  groupId: string
}

const props = defineProps<Props>()
const emit = defineEmits<{ 'update:open': [value: boolean]; disbanded: [] }>()

const REQUIRED_PHRASE = 'DISBAND'
const typed = ref('')
const loading = ref(false)

watch(() => props.open, (isOpen) => {
  if (isOpen) typed.value = ''
})

const matches = computed(() => typed.value.trim() === REQUIRED_PHRASE)
const showMismatch = computed(() => typed.value.length > 0 && !matches.value)

async function onConfirm() {
  if (!matches.value || loading.value) return
  loading.value = true
  try {
    await disbandGroup(props.groupId)
    toast.success('Group disbanded.')
    emit('disbanded')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not disband group.')
    }
  } finally {
    loading.value = false
  }
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
          Disband this group?
        </DialogTitle>
      </DialogHeader>

      <div class="space-y-3">
        <p class="text-sm leading-relaxed text-muted-foreground">
          You are about to disband your group.
        </p>

        <div class="rounded-md border-l-2 border-destructive/60 bg-destructive/5 px-3 py-2.5">
          <p class="text-xs font-medium text-destructive">This action is irreversible.</p>
          <p class="mt-0.5 text-xs leading-relaxed text-destructive/85">
            All members will be removed and all pending invitations will be cancelled.
          </p>
        </div>
      </div>

      <div class="space-y-1.5 pt-1">
        <Label for="disband-confirm" class="text-xs">
          Type <strong>{{ REQUIRED_PHRASE }}</strong> to confirm
        </Label>
        <div class="relative">
          <Input
            id="disband-confirm"
            v-model="typed"
            :placeholder="REQUIRED_PHRASE"
            autocomplete="off"
            spellcheck="false"
            :class="[
              'pr-9',
              showMismatch && 'border-destructive focus-visible:ring-destructive/30',
              matches && 'border-emerald-500/60 focus-visible:ring-emerald-500/20',
            ]"
          />
          <span v-if="matches" class="absolute right-2.5 top-1/2 -translate-y-1/2 text-emerald-500" aria-hidden="true">
            <Check class="size-4" />
          </span>
        </div>
      </div>

      <DialogFooter class="gap-2 sm:gap-0">
        <Button type="button" variant="outline" size="sm" :disabled="loading" @click="emit('update:open', false)">
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
          Disband group
        </Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
