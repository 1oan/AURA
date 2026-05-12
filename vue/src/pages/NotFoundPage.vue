<script setup lang="ts">
import { useRouter } from 'vue-router'
import { Button } from '@/components/ui/button'
import { ArrowLeft, Compass } from 'lucide-vue-next'

const router = useRouter()

function goBack() {
  if (window.history.length > 1) {
    router.back()
  } else {
    router.push('/')
  }
}
</script>

<template>
  <main class="flex min-h-[100dvh] items-center justify-center px-6 py-16">
    <div class="max-w-md text-center">
      <div class="mx-auto mb-6 flex size-12 items-center justify-center rounded-md bg-muted">
        <Compass class="size-6 text-muted-foreground" />
      </div>

      <p class="font-mono text-xs uppercase tracking-[0.2em] text-muted-foreground">
        404 · page not found
      </p>

      <h1 class="mt-3 text-balance text-3xl font-semibold tracking-tight leading-tight">
        That page doesn't exist
      </h1>

      <p class="mt-3 text-balance text-sm text-muted-foreground leading-relaxed">
        The link you followed may be broken, or the page may have been moved.
        Head back to the dashboard to continue where you left off.
      </p>

      <div class="mt-6 flex flex-wrap items-center justify-center gap-2">
        <Button variant="outline" size="sm" class="h-8 text-xs" @click="goBack">
          <ArrowLeft class="mr-1.5 size-3.5" />
          Go back
        </Button>
        <Button size="sm" class="h-8 text-xs" as-child>
          <RouterLink to="/">
            Dashboard
          </RouterLink>
        </Button>
      </div>
    </div>
  </main>
</template>
