<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { BedDouble, Loader2, ArrowRight } from 'lucide-vue-next'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/api/client'
import { placeMeNow } from '@/api/rooms'
import PlaceMeNowConfirmDialog from './PlaceMeNowConfirmDialog.vue'

interface Props {
  groupStatus: 'no-group' | 'forming-leader' | 'forming-member' | 'locked' | 'assigned'
  /**
   * Visual treatment:
   * - 'panel': standalone primary CTA with surrounding card (default for no-group)
   * - 'inline': just the button, no wrapper — for when the caller provides its own card
   */
  display?: 'panel' | 'inline'
}

const props = withDefaults(defineProps<Props>(), { display: 'panel' })
const emit = defineEmits<{ placed: [] }>()

const dialogOpen = ref(false)
const placing = ref(false)

const dialogVariant = computed(() => {
  if (props.groupStatus === 'forming-leader') return 'leader' as const
  if (props.groupStatus === 'forming-member') return 'member' as const
  return 'no-group' as const
})

const isPrimary = computed(() => props.groupStatus === 'no-group')
const usePanel = computed(() => isPrimary.value && props.display === 'panel')

function onButtonClick() {
  dialogOpen.value = true
}

async function onConfirm() {
  dialogOpen.value = false
  placing.value = true
  try {
    await placeMeNow()
    toast.success('Room assigned.')
    emit('placed')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      toast.error(data.detail ?? 'Could not assign a room.')
    }
  } finally {
    placing.value = false
  }
}

function onCancel() {
  dialogOpen.value = false
}
</script>

<template>
  <template v-if="props.groupStatus !== 'assigned' && props.groupStatus !== 'locked'">
    <!-- Standalone primary CTA panel (used on the lobby page) -->
    <section
      v-if="usePanel"
      class="relative overflow-hidden rounded-xl border border-primary/20 bg-gradient-to-br from-primary/[0.08] via-primary/[0.04] to-transparent p-5"
    >
      <div
        class="pointer-events-none absolute -right-16 -bottom-16 size-56 rounded-full opacity-50"
        style="background: radial-gradient(circle, oklch(0.5 0.18 259 / 0.18) 0%, transparent 70%)"
        aria-hidden="true"
      />

      <div class="relative flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div class="flex items-start gap-3">
          <span
            class="flex size-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 ring-1 ring-primary/20"
            aria-hidden="true"
          >
            <BedDouble class="size-5 text-primary" />
          </span>
          <div>
            <h3 class="text-sm font-semibold tracking-tight">Skip the group — get a room now</h3>
            <p class="mt-0.5 max-w-md text-xs text-muted-foreground">
              We'll place you in the best available room in your dormitory. You can't undo this.
            </p>
          </div>
        </div>

        <Button
          size="default"
          :disabled="placing"
          class="group shrink-0 transition-transform active:scale-[0.98]"
          @click="onButtonClick"
        >
          <Loader2 v-if="placing" class="mr-1.5 size-4 animate-spin" />
          <span>{{ placing ? 'Placing...' : 'Get a room' }}</span>
          <ArrowRight v-if="!placing" class="ml-1 size-4 transition-transform group-hover:translate-x-0.5" />
        </Button>
      </div>
    </section>

    <!-- Inline primary button (used when the caller provides its own card) -->
    <Button
      v-else-if="isPrimary"
      size="sm"
      :disabled="placing"
      class="group transition-transform active:scale-[0.98]"
      @click="onButtonClick"
    >
      <Loader2 v-if="placing" class="mr-1 size-3.5 animate-spin" />
      <span>{{ placing ? 'Placing...' : 'Place me now' }}</span>
      <ArrowRight v-if="!placing" class="ml-1 size-3.5 transition-transform group-hover:translate-x-0.5" />
    </Button>

    <!-- Secondary inline trigger: user is in a forming group -->
    <Button
      v-else
      size="sm"
      variant="ghost"
      class="h-8 text-xs text-muted-foreground hover:text-foreground transition-transform active:scale-[0.98]"
      :disabled="placing"
      @click="onButtonClick"
    >
      <Loader2 v-if="placing" class="mr-1 size-3.5 animate-spin" />
      <BedDouble v-else class="mr-1 size-3.5" />
      Or place me solo
    </Button>

    <PlaceMeNowConfirmDialog
      :open="dialogOpen"
      :variant="dialogVariant"
      @update:open="dialogOpen = $event"
      @confirm="onConfirm"
      @cancel="onCancel"
    />
  </template>
</template>
