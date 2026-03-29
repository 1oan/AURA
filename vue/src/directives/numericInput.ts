import type { Directive } from 'vue'

export const vNumeric: Directive = {
  mounted(el: HTMLElement) {
    const input = el.tagName === 'INPUT' ? el as HTMLInputElement : el.querySelector('input')
    if (!input) return

    input.addEventListener('beforeinput', (e: InputEvent) => {
      if (e.data && !/^\d$/.test(e.data)) e.preventDefault()
    })
  },
}
