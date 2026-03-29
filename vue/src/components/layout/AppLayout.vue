<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import { Separator } from '@/components/ui/separator'
import AppSidebar from './AppSidebar.vue'

const route = useRoute()

const pageTitle = computed(() => {
  const titles: Record<string, string> = {
    '/': 'Dashboard',
    '/campuses': 'Campuses & Dormitories',
    '/faculties': 'Faculties',
    '/room-quotas': 'Room Quotas',
    '/allocation': 'Allocation Period',
    '/students': 'Students',
    '/preferences': 'Preferences',
    '/room-assignment': 'Room Assignment',
    '/confirmations': 'Confirmations',
    '/settings': 'Settings',
  }
  return titles[route.path] ?? ''
})
</script>

<template>
  <SidebarProvider>
    <AppSidebar />
    <SidebarInset>
      <header
        class="flex h-12 shrink-0 items-center gap-2 border-b transition-[width,height] ease-linear group-has-data-[collapsible=icon]/sidebar-wrapper:h-10"
      >
        <div class="flex items-center gap-2.5 px-4">
          <SidebarTrigger class="-ml-1 size-6 text-muted-foreground hover:text-foreground" />
          <Separator orientation="vertical" class="h-3.5" />
          <span class="text-[13px] font-medium text-foreground/70">{{ pageTitle }}</span>
        </div>
      </header>
      <main class="flex-1 overflow-auto p-6">
        <slot />
      </main>
    </SidebarInset>
  </SidebarProvider>
</template>