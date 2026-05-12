<script setup lang="ts">
import { ref, computed } from 'vue'
import { toast } from 'vue-sonner'
import { Button } from '@/components/ui/button'
import {
  PhCircleNotch as CircleNotch,
  PhMusicNotesSimple as MusicNotesSimple,
  PhSpotifyLogo as SpotifyLogo,
  PhCheckCircle as CheckCircle,
} from '@phosphor-icons/vue'
import { ApiError } from '@/api/client'
import { startSpotifyConnect, disconnectSpotify } from '@/api/profile'

interface Props {
  connected: boolean
  connectedAt?: string | null
  scopeCount?: number
}

const props = defineProps<Props>()
const emit = defineEmits<{ disconnected: [] }>()

const loading = ref(false)
const error = ref('')
const confirming = ref(false)

const connectedSince = computed(() => {
  if (!props.connectedAt) return ''
  try {
    const d = new Date(props.connectedAt)
    return d.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
  } catch {
    return ''
  }
})

async function handleConnect() {
  loading.value = true
  error.value = ''
  try {
    const result = await startSpotifyConnect()
    window.location.href = result.authorizationUrl
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not start Spotify connect.'
    } else {
      error.value = 'Could not start Spotify connect.'
    }
    loading.value = false
  }
}

async function confirmDisconnect() {
  loading.value = true
  error.value = ''
  try {
    await disconnectSpotify()
    toast.success('Spotify disconnected.')
    emit('disconnected')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Could not disconnect Spotify.'
    } else {
      error.value = 'Could not disconnect Spotify.'
    }
  } finally {
    loading.value = false
    confirming.value = false
  }
}
</script>

<template>
  <div class="space-y-4">
    <template v-if="!connected">
      <div class="rounded-lg border bg-gradient-to-br from-emerald-500/5 via-transparent to-transparent p-5">
        <div class="flex items-start gap-3">
          <span class="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-500/10 text-emerald-600 dark:text-emerald-300">
            <MusicNotesSimple :size="20" weight="duotone" />
          </span>
          <div class="space-y-1">
            <p class="text-[15px] font-medium leading-tight">Connect your Spotify account</p>
            <p class="text-[13px] text-muted-foreground">
              We capture your top artists, tracks, and genres at connect time. You can disconnect anytime, and we only request read access to your top listening history.
            </p>
          </div>
        </div>
      </div>

      <Button
        type="button"
        class="w-full bg-emerald-600 text-white shadow-md shadow-emerald-500/20 transition-all hover:bg-emerald-700 active:scale-[0.98] dark:bg-emerald-500 dark:hover:bg-emerald-400"
        :disabled="loading"
        @click="handleConnect"
      >
        <CircleNotch v-if="loading" :size="16" class="mr-2 animate-spin" />
        <SpotifyLogo v-else :size="16" weight="fill" class="mr-2" />
        {{ loading ? 'Opening Spotify…' : 'Continue with Spotify' }}
      </Button>
    </template>

    <template v-else>
      <div class="rounded-lg border border-emerald-500/30 bg-emerald-500/[0.04] p-5">
        <div class="flex items-start gap-3">
          <span class="flex size-10 shrink-0 items-center justify-center rounded-lg bg-emerald-500/15 text-emerald-600 dark:text-emerald-300">
            <CheckCircle :size="20" weight="duotone" />
          </span>
          <div class="space-y-1">
            <p class="text-[15px] font-medium leading-tight">Connected to Spotify</p>
            <p class="text-[13px] text-muted-foreground">
              <template v-if="connectedSince">Snapshot captured on {{ connectedSince }}.</template>
              <template v-else>Your listening snapshot is stored and ready.</template>
              The AI matching module will use this when it ships.
            </p>
            <p v-if="scopeCount" class="pt-1 text-[12px] text-muted-foreground/80 tabular-nums">
              {{ scopeCount }} {{ scopeCount === 1 ? 'permission' : 'permissions' }} granted (read-only).
            </p>
          </div>
        </div>
      </div>

      <div v-if="!confirming" class="flex items-center gap-2">
        <Button
          type="button"
          variant="outline"
          class="w-full transition-transform active:scale-[0.98]"
          :disabled="loading"
          @click="confirming = true"
        >
          Disconnect
        </Button>
      </div>

      <div
        v-else
        class="flex items-center gap-2 rounded-lg border border-destructive/30 bg-destructive/5 p-3 animate-in fade-in slide-in-from-bottom-1 duration-200"
      >
        <p class="flex-1 text-[13px] text-foreground">Disconnect Spotify? Your music data won't be used for matching.</p>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          class="h-8 text-xs"
          :disabled="loading"
          @click="confirming = false"
        >
          Keep
        </Button>
        <Button
          type="button"
          variant="destructive"
          size="sm"
          class="h-8 text-xs"
          :disabled="loading"
          @click="confirmDisconnect"
        >
          <CircleNotch v-if="loading" :size="14" class="mr-1 animate-spin" />
          Disconnect
        </Button>
      </div>
    </template>

    <p v-if="error" class="text-xs text-destructive" role="alert">{{ error }}</p>
  </div>
</template>
