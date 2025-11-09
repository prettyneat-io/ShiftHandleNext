// theme.config.ts
// ShiftHandle Next - Precision workforce orchestration platform
// Updated to match frontend-style-guide.md

export const themes = {
  shiftHandle: {
    name: "ShiftHandle",
    typography: {
      branding: 'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
      ui:       'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
    },
    colors: {
      primary: {
        DEFAULT: "#14647A", // Main actions, highlights, links
        alt:     "#1F7CA8", // Hover states, secondary emphasis (primary-alt)
        light:   "#1F7CA8", // Kept for backwards compatibility
        dark:    "#14647A",
      },
      secondary: {
        DEFAULT: "#65708A", // Secondary buttons, tabs, muted icons
        light:   "#8899B0",
        dark:    "#4D596A",
      },
      background: {
        DEFAULT: "#F5F7FA", // Page surface
        alt:     "#E6EAEE", // Alternating rows, card fills (background-alt)
        light:   "#F5F7FA",
        dark:    "#111820", // Primary dark surface (background-dark)
      },
      accent: {
        DEFAULT: "#3C73D3", // Info banners, analytics cards (info)
        light:   "#5A8FE8",
        dark:    "#2B5AA8",
      },
      danger: {
        DEFAULT: "#B73131", // Destructive icons/text only (danger-accent)
        light:   "#D45C5C",
        dark:    "#8F2626",
      },
      success: {
        DEFAULT: "#1F8A56", // Healthy status, clocked in
        light:   "#34A874",
        dark:    "#176B44",
      },
      warning: {
        DEFAULT: "#D2951C", // Late arrivals, pending review
        light:   "#E5B14D",
        dark:    "#A87616",
      },
      neutral: {
        DEFAULT: "#C6CED8", // Borders, separators (neutral-light)
        light:   "#C6CED8",
        mid:     "#4D596A", // Labels, icons (neutral-mid)
        dark:    "#273141", // Primary text (neutral-dark)
      },
      // Dark mode specific tokens
      panel: {
        DEFAULT: "#1A2330",
        light:   "#1A2330",
        dark:    "#1A2330", // Cards, modals in dark mode
      },
      text: {
        DEFAULT:      "#F1F5F9",
        light:        "#F1F5F9",
        dark:         "#CBD5E1", // Secondary text in dark mode
        darkMuted:    "#CBD5E1", // Backwards compatibility alias
      },
      border: {
        DEFAULT: "#2E3A4A",
        light:   "#2E3A4A",
        dark:    "#2E3A4A", // Borders in dark mode
      },
    },
    effects: {
      glow:      "0 0 0 3px rgba(20, 100, 122, 0.25)",
      hoverGlow: "0 0 0 4px rgba(31, 124, 168, 0.20)",
    },
  },
} as const

export type ThemeKey = keyof typeof themes

export const defaultThemeKey: ThemeKey = 'shiftHandle'

const currentTheme = themes[defaultThemeKey]

export default {
  themes,
  currentTheme,
  defaultThemeKey,
}
