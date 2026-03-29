import './assets/main.css'

import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import { router } from './router'
import { vNumeric } from './directives/numericInput'

const app = createApp(App)

app.use(createPinia())
app.use(router)
app.directive('numeric', vNumeric)

app.mount('#app')