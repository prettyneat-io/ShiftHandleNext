import { computed, ref } from 'vue'
import themeConfig, { type ThemeKey, defaultThemeKey } from '../theme.config'

const STORAGE_KEY = 'ab-cultivation-theme'

const currentThemeKey = ref<ThemeKey>(defaultThemeKey)
let initialized = false

const themeEntries = Object.entries(themeConfig.themes) as Array<[
  ThemeKey,
  (typeof themeConfig.themes)[ThemeKey]
]>

const toRgb = (hex: string) => {
  const sanitized = hex.replace('#', '')
  const bigint = parseInt(sanitized, 16)
  const r = (bigint >> 16) & 255
  const g = (bigint >> 8) & 255
  const b = bigint & 255
  return `${r} ${g} ${b}`
}

const setColorVariables = (themeKey: ThemeKey) => {
  if (!import.meta.client) return

  const theme = themeConfig.themes[themeKey]

  if (!theme) return

  const root = document.documentElement

  ;(Object.entries(theme.colors) as Array<[
    string,
    { DEFAULT?: string; light?: string; dark?: string }
  ]>).forEach(([token, variants]) => {
    if (!variants?.DEFAULT) {
      return
    }

    const defaultColor = variants.DEFAULT
    const lightColor = variants.light ?? defaultColor
    const darkColor = variants.dark ?? defaultColor

    root.style.setProperty(`--theme-${token}`, toRgb(defaultColor))
    root.style.setProperty(`--theme-${token}-light`, toRgb(lightColor))
    root.style.setProperty(`--theme-${token}-dark`, toRgb(darkColor))
  })

  root.style.setProperty('--theme-shadow-glow', theme.effects.glow)
  root.style.setProperty('--theme-shadow-hover', theme.effects.hoverGlow)
  root.setAttribute('data-theme', themeKey)
}

export function useThemeManager() {
  const availableThemes = computed<Array<{ key: ThemeKey; name: string }>>(() =>
    themeEntries.map(([key, value]) => ({ key, name: value.name }))
  )

  const currentTheme = computed(() =>
    availableThemes.value.find((theme) => theme.key === currentThemeKey.value)
  )

  const applyTheme = (themeKey: ThemeKey) => {
    if (!process.client) return
    if (!themeConfig.themes[themeKey]) return

    setColorVariables(themeKey)
    localStorage.setItem(STORAGE_KEY, themeKey)
    currentThemeKey.value = themeKey
  }

  const initialize = () => {
    if (initialized || !process.client) return
    initialized = true
    const stored = localStorage.getItem(STORAGE_KEY) as ThemeKey | null
    const key = stored && themeConfig.themes[stored] ? stored : defaultThemeKey
    setColorVariables(key)
    currentThemeKey.value = key
  }

  const setTheme = (themeKey: ThemeKey) => {
    initialize()
    applyTheme(themeKey)
  }

  return {
    availableThemes,
    currentThemeKey,
    currentTheme,
    setTheme,
    initialize,
  }
}
