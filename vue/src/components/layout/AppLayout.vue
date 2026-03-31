<script setup lang="ts">
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@/components/ui/sidebar'
import { Separator } from '@/components/ui/separator'
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb'
import AppSidebar from './AppSidebar.vue'

const route = useRoute()

const breadcrumbs = computed(() => {
  const titles: Record<string, string> = {
    '/': 'Dashboard',
    '/campuses': 'Campuses',
    '/faculties': 'Faculties',
    '/room-quotas': 'Room Quotas',
    '/allocation': 'Allocation Periods',
    '/students': 'Students',
    '/preferences': 'Preferences',
    '/room-assignment': 'Room Assignment',
    '/confirmations': 'Confirmations',
    '/settings': 'Settings',
  }
  return [{ label: titles[route.path] ?? '', path: route.path }]
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
          <SidebarTrigger class="-ml-1 size-5 text-muted-foreground hover:text-foreground" />
          <Separator orientation="vertical" class="h-3.5" />
          <Breadcrumb>
            <BreadcrumbList>
              <BreadcrumbItem v-for="(crumb, i) in breadcrumbs" :key="crumb.path">
                <BreadcrumbSeparator v-if="i > 0" />
                <BreadcrumbPage v-if="i === breadcrumbs.length - 1" class="text-[13px] font-medium">
                  {{ crumb.label }}
                </BreadcrumbPage>
                <BreadcrumbLink v-else :href="crumb.path" class="text-[13px]">
                  {{ crumb.label }}
                </BreadcrumbLink>
              </BreadcrumbItem>
            </BreadcrumbList>
          </Breadcrumb>
        </div>
      </header>
      <main class="flex-1 overflow-auto p-3">
        <slot />
      </main>
    </SidebarInset>
  </SidebarProvider>
</template>