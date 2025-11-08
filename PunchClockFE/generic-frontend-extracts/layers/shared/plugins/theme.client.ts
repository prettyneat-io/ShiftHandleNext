import { defineNuxtPlugin } from '#app'
import { useThemeManager } from '../composables/useThemeManager'

export default defineNuxtPlugin(() => {
  const { initialize } = useThemeManager()
  initialize()
})
