<script setup lang="ts">
import AuthLayout from '@/components/layout/AuthLayout.vue'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
</script>

<template>
  <AuthLayout>
    <div class="space-y-6">
      <div class="space-y-2">
        <h1 class="text-2xl font-semibold tracking-tight">Welcome back</h1>
        <p class="text-sm text-muted-foreground">
          Sign in to your account to continue.
        </p>
      </div>

      <div class="space-y-4">
        <div class="space-y-2">
          <label class="text-sm font-medium" for="email">Email</label>
          <Input
            id="email"
            type="email"
            placeholder="name@uaic.ro"
            disabled
          />
        </div>
        <div class="space-y-2">
          <label class="text-sm font-medium" for="password">Password</label>
          <Input
            id="password"
            type="password"
            placeholder="Enter your password"
            disabled
          />
        </div>
        <Button class="w-full" disabled>
          Sign in
        </Button>
      </div>

      <p class="text-center text-xs text-muted-foreground">
        Authentication will be available in a future update.
      </p>
    </div>
  </AuthLayout>
</template>
