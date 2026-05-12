<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { PhCircleNotch as CircleNotch } from '@phosphor-icons/vue'
import { ApiError } from '@/api/client'
import { setInterests } from '@/api/profile'
import { getInterestCatalog } from '@/api/interests'
import type { InterestCategoryDto } from '@/api/interests'

interface Props {
  initialSlugs: string[]
}

const props = defineProps<Props>()
const emit = defineEmits<{ saved: [] }>()

const catalog = ref<InterestCategoryDto[]>([])
const selected = ref<Set<string>>(new Set(props.initialSlugs))
const loading = ref(true)
const saving = ref(false)
const error = ref('')

const selectedCount = computed(() => selected.value.size)

onMounted(async () => {
  try {
    catalog.value = await getInterestCatalog()
  } catch {
    error.value = 'Could not load interests.'
  } finally {
    loading.value = false
  }
})

function toggle(slug: string) {
  // Vue 3's reactivity proxy instruments Set.add/Set.delete, so mutating in place
  // triggers updates on `selected.value.size` and `.has(...)` reads inside the template.
  if (selected.value.has(slug)) selected.value.delete(slug)
  else selected.value.add(slug)
}

function categoryLabel(cat: string): string {
  return cat.charAt(0).toUpperCase() + cat.slice(1)
}

async function handleSave() {
  saving.value = true
  error.value = ''
  try {
    await setInterests(Array.from(selected.value))
    toast.success('Interests saved.')
    emit('saved')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not save interests.'
    } else {
      error.value = 'Could not save interests.'
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="space-y-5">
    <div v-if="loading" class="space-y-3">
      <Skeleton class="h-4 w-24" />
      <div class="flex flex-wrap gap-1.5">
        <Skeleton v-for="i in 8" :key="i" class="h-7 w-16 rounded-md" />
      </div>
      <Skeleton class="h-4 w-24" />
      <div class="flex flex-wrap gap-1.5">
        <Skeleton v-for="i in 6" :key="i" class="h-7 w-20 rounded-md" />
      </div>
    </div>

    <template v-else-if="catalog.length === 0">
      <div class="rounded-md border border-dashed p-6 text-center">
        <p class="text-sm font-medium">No interests available</p>
        <p class="mt-1 text-xs text-muted-foreground">Contact your faculty admin.</p>
      </div>
    </template>

    <template v-else>
      <div v-for="cat in catalog" :key="cat.category" class="space-y-2">
        <p class="text-[13px] font-medium text-foreground">
          {{ categoryLabel(cat.category) }}
        </p>
        <div class="flex flex-wrap gap-1.5">
          <Button
            v-for="item in cat.items"
            :key="item.slug"
            type="button"
            size="sm"
            :variant="selected.has(item.slug) ? 'default' : 'outline'"
            class="h-7 text-xs"
            @click="toggle(item.slug)"
          >
            {{ item.label }}
          </Button>
        </div>
      </div>
    </template>

    <p v-if="error" class="text-xs text-destructive" role="alert">{{ error }}</p>

    <div class="flex items-center gap-3 border-t border-border/60 pt-4">
      <p class="text-[13px] text-muted-foreground tabular-nums">
        <span class="font-medium text-foreground">{{ selectedCount }}</span> selected
      </p>
      <Button
        type="button"
        class="ml-auto transition-transform active:scale-[0.98]"
        :disabled="loading || saving"
        @click="handleSave"
      >
        <CircleNotch v-if="saving" :size="14" class="mr-2 animate-spin" />
        {{ saving ? 'Saving…' : 'Save' }}
      </Button>
    </div>
  </div>
</template>
