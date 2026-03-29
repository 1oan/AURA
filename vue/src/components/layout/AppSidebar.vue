<script setup lang="ts">
import { computed, type Component } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import {
  LayoutDashboard,
  Building2,
  GraduationCap,
  Grid3X3,
  CalendarClock,
  Users,
  ListOrdered,
  DoorOpen,
  CheckCircle2,
  Settings,
  ChevronsUpDown,
  LogOut,
} from 'lucide-vue-next'
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  SidebarSeparator,
} from '@/components/ui/sidebar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import ThemeToggle from './ThemeToggle.vue'

interface NavItem {
  title: string
  url: string
  icon: Component
  roles?: string[]
}

const route = useRoute()
const authStore = useAuthStore()

const userRole = computed(() => authStore.user?.role ?? '')

const mainNav: NavItem[] = [
  { title: 'Dashboard', url: '/', icon: LayoutDashboard },
]

const managementNav: NavItem[] = [
  { title: 'Campuses', url: '/campuses', icon: Building2 },
  { title: 'Faculties', url: '/faculties', icon: GraduationCap },
  { title: 'Room Quotas', url: '/room-quotas', icon: Grid3X3 },
  { title: 'Allocation Period', url: '/allocation', icon: CalendarClock },
]

const allocationNav: NavItem[] = [
  { title: 'Students', url: '/students', icon: Users, roles: ['SuperAdmin', 'FacultyAdmin'] },
  { title: 'Preferences', url: '/preferences', icon: ListOrdered },
  { title: 'Room Assignment', url: '/room-assignment', icon: DoorOpen, roles: ['SuperAdmin', 'FacultyAdmin'] },
  { title: 'Confirmations', url: '/confirmations', icon: CheckCircle2 },
]

const isAdmin = computed(() => ['SuperAdmin', 'FacultyAdmin'].includes(userRole.value))

function visibleItems(items: NavItem[]) {
  return items.filter(item => !item.roles || item.roles.includes(userRole.value))
}

const currentPath = computed(() => route.path)

function isActive(url: string) {
  if (url === '/') return currentPath.value === '/'
  return currentPath.value.startsWith(url)
}
</script>

<template>
  <Sidebar collapsible="icon" class="border-r">
    <SidebarHeader class="p-4">
      <SidebarMenu>
        <SidebarMenuItem>
          <SidebarMenuButton size="lg" as-child class="hover:bg-transparent active:bg-transparent">
            <router-link to="/" class="flex items-center gap-3">
              <div
                class="flex aspect-square size-8 items-center justify-center rounded-md bg-primary text-sm font-bold text-primary-foreground"
              >
                A
              </div>
              <div class="grid flex-1 text-left leading-tight">
                <span class="text-sm font-semibold tracking-tight">AURA</span>
                <span class="text-[10px] font-medium uppercase tracking-[0.15em] text-muted-foreground">
                  Room Allocation
                </span>
              </div>
            </router-link>
          </SidebarMenuButton>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarHeader>

    <SidebarSeparator class="mx-4" />

    <SidebarContent class="px-2 pt-2">
      <SidebarGroup>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in mainNav" :key="item.title">
              <SidebarMenuButton
                as-child
                :data-active="isActive(item.url)"
                class="h-8 transition-colors duration-100"
              >
                <router-link :to="item.url">
                  <component :is="item.icon" class="size-4" />
                  <span class="text-[13px]">{{ item.title }}</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>

      <SidebarGroup v-if="isAdmin">
        <SidebarGroupLabel class="mb-0.5 px-2 text-[10px] font-semibold uppercase tracking-[0.12em] text-muted-foreground/60">
          Management
        </SidebarGroupLabel>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in managementNav" :key="item.title">
              <SidebarMenuButton
                as-child
                :data-active="isActive(item.url)"
                class="h-8 transition-colors duration-100"
              >
                <router-link :to="item.url">
                  <component :is="item.icon" class="size-4" />
                  <span class="text-[13px]">{{ item.title }}</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>

      <SidebarGroup>
        <SidebarGroupLabel class="mb-0.5 px-2 text-[10px] font-semibold uppercase tracking-[0.12em] text-muted-foreground/60">
          Allocation
        </SidebarGroupLabel>
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem v-for="item in visibleItems(allocationNav)" :key="item.title">
              <SidebarMenuButton
                as-child
                :data-active="isActive(item.url)"
                class="h-8 transition-colors duration-100"
              >
                <router-link :to="item.url">
                  <component :is="item.icon" class="size-4" />
                  <span class="text-[13px]">{{ item.title }}</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>

      <SidebarGroup class="mt-auto">
        <SidebarGroupContent>
          <SidebarMenu>
            <SidebarMenuItem>
              <SidebarMenuButton
                as-child
                :data-active="isActive('/settings')"
                class="h-8 transition-colors duration-100"
              >
                <router-link to="/settings">
                  <Settings class="size-4" />
                  <span class="text-[13px]">Settings</span>
                </router-link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          </SidebarMenu>
        </SidebarGroupContent>
      </SidebarGroup>
    </SidebarContent>

    <SidebarSeparator class="mx-4" />

    <SidebarFooter class="p-3">
      <SidebarMenu>
        <SidebarMenuItem>
          <DropdownMenu>
            <DropdownMenuTrigger as-child>
              <SidebarMenuButton
                size="lg"
                class="w-full transition-colors duration-100"
              >
                <Avatar class="size-7 rounded-md">
                  <AvatarFallback class="rounded-md bg-primary/10 text-[11px] font-semibold text-primary">
                    {{ authStore.user?.firstName?.[0] ?? '' }}{{ authStore.user?.lastName?.[0] ?? '' }}
                  </AvatarFallback>
                </Avatar>
                <div class="grid flex-1 text-left text-sm leading-tight">
                  <span class="truncate text-[13px] font-medium">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</span>
                  <span class="truncate text-[11px] text-muted-foreground">{{ authStore.user?.email }}</span>
                </div>
                <ChevronsUpDown class="ml-auto size-4 text-muted-foreground" />
              </SidebarMenuButton>
            </DropdownMenuTrigger>
            <DropdownMenuContent
              class="w-[--reka-popper-anchor-width] min-w-56"
              side="top"
              align="start"
            >
              <DropdownMenuItem class="gap-2">
                <Settings class="size-4" />
                Settings
              </DropdownMenuItem>
              <DropdownMenuItem class="gap-2">
                <ThemeToggle />
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem class="gap-2 text-destructive" @click="authStore.logout()">
                <LogOut class="size-4" />
                Sign out
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </SidebarMenuItem>
      </SidebarMenu>
    </SidebarFooter>

    <SidebarRail />
  </Sidebar>
</template>