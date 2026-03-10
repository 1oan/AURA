import { ref, watch, onMounted, onUnmounted } from 'vue'

type Theme = 'light' | 'dark' | 'system'

const STORAGE_KEY = 'aura-theme'

const theme = ref<Theme>((localStorage.getItem(STORAGE_KEY) as Theme) ?? 'system')

function applyTheme(value: Theme) {
  const isDark =
    value === 'dark' || (value === 'system' && globalThis.matchMedia('(prefers-color-scheme: dark)').matches)

  document.documentElement.classList.toggle('dark', isDark)
}

export function useTheme() {
  let mediaQuery: MediaQueryList | undefined

  function onSystemChange() {
    if (theme.value === 'system') {
      applyTheme('system')
    }
  }

  watch(theme, (value) => {
    localStorage.setItem(STORAGE_KEY, value)
    applyTheme(value)
  })

  onMounted(() => {
    applyTheme(theme.value)
    mediaQuery = globalThis.matchMedia('(prefers-color-scheme: dark)')
    mediaQuery.addEventListener('change', onSystemChange)
  })

  onUnmounted(() => {
    mediaQuery?.removeEventListener('change', onSystemChange)
  })

  return { theme }
}