<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { toast } from 'vue-sonner'
import {
  PhArmchair as Armchair,
  PhCompass as Compass,
  PhBookmarks as Bookmarks,
  PhMusicNotesSimple as MusicNotesSimple,
} from '@phosphor-icons/vue'

import AppLayout from '@/components/layout/AppLayout.vue'
import { Skeleton } from '@/components/ui/skeleton'
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from '@/components/ui/collapsible'
import { getMyProfile } from '@/api/profile'
import type { ProfileDto } from '@/api/profile'

import ProfileSectionRow from '@/components/features/profile/ProfileSectionRow.vue'
import ProfileProgressRing from '@/components/features/profile/ProfileProgressRing.vue'
import LifestyleSectionForm from '@/components/features/profile/LifestyleSectionForm.vue'
import TipiSectionForm from '@/components/features/profile/TipiSectionForm.vue'
import InterestsSectionForm from '@/components/features/profile/InterestsSectionForm.vue'
import SpotifyConnectSection from '@/components/features/profile/SpotifyConnectSection.vue'

type SectionKey = 'lifestyle' | 'tipi' | 'interests' | 'spotify'

const profile = ref<ProfileDto | null>(null)
const loading = ref(true)
const openSection = ref<SectionKey | null>(null)
const route = useRoute()

async function refresh() {
  profile.value = await getMyProfile()
}

onMounted(async () => {
  try {
    await refresh()
    if (route.query.spotify === 'connected') {
      toast.success('Spotify connected.')
      openSection.value = 'spotify'
    }
  } finally {
    loading.value = false
  }
})

const doneMap = computed(() => ({
  lifestyle: !!profile.value?.lifestyle,
  tipi: !!profile.value?.tipi,
  interests: !!profile.value?.interests.completedAt,
  spotify: !!profile.value?.spotify.connected,
}))

const completedCount = computed(() =>
  Object.values(doneMap.value).filter(Boolean).length,
)

const completeness = computed(() => profile.value?.completenessPercent ?? 0)

const nextSectionName = computed<string | null>(() => {
  const order: Array<[SectionKey, string]> = [
    ['lifestyle', 'lifestyle'],
    ['tipi', 'personality'],
    ['interests', 'interests'],
    ['spotify', 'Spotify'],
  ]
  const missing = order.find(([k]) => !doneMap.value[k])
  return missing ? missing[1] : null
})

const nudge = computed(() => {
  if (loading.value) return ''
  if (completedCount.value === 0) return 'Start with lifestyle — it powers the strongest matches.'
  if (completedCount.value === 4) return 'All four sections complete. You\'ll get the best matches available.'
  return `Add ${nextSectionName.value} to keep improving your matches.`
})

function pillFor(section: SectionKey): string {
  if (!profile.value) return 'Not started'
  if (section === 'lifestyle') return profile.value.lifestyle ? 'Complete' : 'Not started'
  if (section === 'tipi') return profile.value.tipi ? 'Complete' : 'Not started'
  if (section === 'interests') {
    const n = profile.value.interests.slugs.length
    return profile.value.interests.completedAt ? `${n} selected` : 'Not started'
  }
  return profile.value.spotify.connected ? 'Connected' : 'Not connected'
}

// Centralised open/close. When the user toggles a section, the controlled prop
// on every other Collapsible flips false simultaneously, so only one is ever
// expanded — without needing per-collapsible event listeners.
function setOpen(key: SectionKey, value: boolean) {
  openSection.value = value ? key : null
}

async function onSaved() {
  await refresh()
  openSection.value = null
}
</script>

<template>
  <AppLayout>
    <div class="max-w-2xl space-y-8">
      <header class="space-y-2">
        <h1 class="text-balance text-4xl font-semibold tracking-[-0.02em] leading-[1.05]">
          Your profile
        </h1>
        <p class="max-w-[60ch] text-balance text-[15px] text-muted-foreground">
          Each section helps the system find better roommates for you.
          Fill what you want, skip what you don't.
        </p>
      </header>

      <section
        v-if="!loading"
        class="flex items-center gap-4 rounded-xl border bg-card/60 p-4 animate-in fade-in slide-in-from-bottom-1 duration-500"
      >
        <ProfileProgressRing :value="completeness" />
        <div class="flex-1 min-w-0 space-y-0.5">
          <p class="text-[13px] font-medium text-foreground">
            {{ completedCount }} of 4 sections complete
          </p>
          <p class="truncate text-[13px] text-muted-foreground">{{ nudge }}</p>
        </div>
      </section>

      <div v-if="loading" class="space-y-2">
        <Skeleton class="h-16 rounded-lg" />
        <Skeleton class="h-16 rounded-lg" />
        <Skeleton class="h-16 rounded-lg" />
        <Skeleton class="h-16 rounded-lg" />
      </div>

      <div
        v-else
        class="overflow-hidden rounded-xl border bg-card divide-y divide-border/60 animate-in fade-in slide-in-from-bottom-2 duration-500 [animation-delay:80ms] [animation-fill-mode:both]"
      >
        <Collapsible
          :open="openSection === 'lifestyle'"
          @update:open="(v: boolean) => setOpen('lifestyle', v)"
        >
          <CollapsibleTrigger as-child>
            <button
              type="button"
              class="w-full text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
            >
              <ProfileSectionRow
                :icon="Armchair"
                title="Lifestyle"
                accent="slate"
                :done="doneMap.lifestyle"
                :status="pillFor('lifestyle')"
                :open="openSection === 'lifestyle'"
              />
            </button>
          </CollapsibleTrigger>
          <CollapsibleContent
            class="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up"
          >
            <div class="border-t border-border/60 px-4 pb-5 pt-5">
              <LifestyleSectionForm
                :initial="profile?.lifestyle ?? null"
                @saved="onSaved"
              />
            </div>
          </CollapsibleContent>
        </Collapsible>

        <Collapsible
          :open="openSection === 'tipi'"
          @update:open="(v: boolean) => setOpen('tipi', v)"
        >
          <CollapsibleTrigger as-child>
            <button
              type="button"
              class="w-full text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
            >
              <ProfileSectionRow
                :icon="Compass"
                title="Personality"
                accent="violet"
                :done="doneMap.tipi"
                :status="pillFor('tipi')"
                :open="openSection === 'tipi'"
              />
            </button>
          </CollapsibleTrigger>
          <CollapsibleContent
            class="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up"
          >
            <div class="border-t border-border/60 px-4 pb-5 pt-5">
              <TipiSectionForm @saved="onSaved" />
            </div>
          </CollapsibleContent>
        </Collapsible>

        <Collapsible
          :open="openSection === 'interests'"
          @update:open="(v: boolean) => setOpen('interests', v)"
        >
          <CollapsibleTrigger as-child>
            <button
              type="button"
              class="w-full text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
            >
              <ProfileSectionRow
                :icon="Bookmarks"
                title="Interests"
                accent="accent"
                :done="doneMap.interests"
                :status="pillFor('interests')"
                :open="openSection === 'interests'"
              />
            </button>
          </CollapsibleTrigger>
          <CollapsibleContent
            class="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up"
          >
            <div class="border-t border-border/60 px-4 pb-5 pt-5">
              <InterestsSectionForm
                :initial-slugs="profile?.interests.slugs ?? []"
                @saved="onSaved"
              />
            </div>
          </CollapsibleContent>
        </Collapsible>

        <Collapsible
          :open="openSection === 'spotify'"
          @update:open="(v: boolean) => setOpen('spotify', v)"
        >
          <CollapsibleTrigger as-child>
            <button
              type="button"
              class="w-full text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-inset"
            >
              <ProfileSectionRow
                :icon="MusicNotesSimple"
                title="Spotify"
                accent="emerald"
                :done="doneMap.spotify"
                :status="pillFor('spotify')"
                :open="openSection === 'spotify'"
              />
            </button>
          </CollapsibleTrigger>
          <CollapsibleContent
            class="overflow-hidden data-[state=open]:animate-collapsible-down data-[state=closed]:animate-collapsible-up"
          >
            <div class="border-t border-border/60 px-4 pb-5 pt-5">
              <SpotifyConnectSection
                :connected="profile?.spotify.connected ?? false"
                :connected-at="profile?.spotify.connectedAt ?? null"
                :scope-count="profile?.spotify.scopes.length ?? 0"
                @disconnected="refresh"
              />
            </div>
          </CollapsibleContent>
        </Collapsible>
      </div>
    </div>
  </AppLayout>
</template>
