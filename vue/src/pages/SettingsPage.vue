<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { toast } from 'vue-sonner'
import AppLayout from '@/components/layout/AppLayout.vue'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { useAuthStore } from '@/stores/auth'
import { setMatriculationCode } from '@/api/auth'
import { ApiError } from '@/api/client'

const authStore = useAuthStore()

const matricCode = ref('')
const saving = ref(false)
const error = ref('')

onMounted(() => {
  matricCode.value = authStore.user?.matriculationCode ?? ''
})

async function saveMatriculationCode() {
  if (!matricCode.value.trim()) {
    error.value = 'Matriculation code is required.'
    return
  }
  error.value = ''
  saving.value = true
  try {
    await setMatriculationCode(matricCode.value.trim())
    await authStore.fetchCurrentUser()
    toast.success('Matriculation code saved.')
  } catch (e) {
    if (e instanceof ApiError) {
      const data = e.data as { detail?: string }
      error.value = data.detail ?? 'Failed to save.'
    }
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <AppLayout>
    <div class="space-y-3">
      <div>
        <h1 class="text-lg font-semibold tracking-tight">Settings</h1>
        <p class="text-xs text-muted-foreground">Manage your profile.</p>
      </div>

      <!-- Profile info -->
      <Card>
        <CardHeader class="p-3 pb-2">
          <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Profile</CardTitle>
        </CardHeader>
        <CardContent class="space-y-3 p-3 pt-0">
          <div class="grid gap-3 sm:grid-cols-2">
            <div class="space-y-1">
              <Label class="text-xs text-muted-foreground">Name</Label>
              <p class="text-sm font-medium">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</p>
            </div>
            <div class="space-y-1">
              <Label class="text-xs text-muted-foreground">Email</Label>
              <p class="text-sm font-mono">{{ authStore.user?.email }}</p>
            </div>
            <div class="space-y-1">
              <Label class="text-xs text-muted-foreground">Role</Label>
              <Badge variant="outline" class="text-xs">{{ authStore.user?.role }}</Badge>
            </div>
          </div>
        </CardContent>
      </Card>

      <!-- Matriculation code -->
      <Card>
        <CardHeader class="p-3 pb-2">
          <CardTitle class="text-xs font-semibold uppercase tracking-wider text-muted-foreground">Matriculation Code</CardTitle>
        </CardHeader>
        <CardContent class="p-3 pt-0">
          <form class="flex items-end gap-2" @submit.prevent="saveMatriculationCode">
            <div class="w-48 space-y-1">
              <Label for="matric-code" class="text-xs">Your university matriculation number</Label>
              <Input
                id="matric-code"
                v-model="matricCode"
                placeholder="e.g. CS2024001"
                class="font-mono"
              />
            </div>
            <Button size="sm" type="submit" :disabled="saving" class="h-9">
              {{ saving ? 'Saving...' : 'Save' }}
            </Button>
          </form>
          <p v-if="error" role="alert" class="mt-2 text-xs text-destructive">{{ error }}</p>
        </CardContent>
      </Card>
    </div>
  </AppLayout>
</template>
