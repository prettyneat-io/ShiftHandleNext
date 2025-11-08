// theme.config.ts
// Pharma-grade default + Seed-to-Sale (agricultural cannabis)
// Structure remains compatible with your Tailwind mapping.

export const themes = {
  pharmaCore: {
    name: "Pharma Core",
    typography: {
      branding: 'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
      ui:       'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
    },
    colors: {
      primary: {
        DEFAULT: "#2F5D9F", // trust blue (base)
        light:   "#3C74B1", // hover
        dark:    "#1F3F71", // active/focus
      },
      secondary: {
        DEFAULT: "#64748B",
        light:   "#94A3B8",
        dark:    "#475569",
      },
      background: {
        DEFAULT: "#FFFFFF",
        light:   "#F7F9FC",
        dark:    "#0F172A", // Dark Slate
      },
      accent: {
        DEFAULT: "#3C74B1",
        light:   "#6B94C8",
        dark:    "#1F3F71",
      },
      danger: {
        DEFAULT: "#C93D3D",
        light:   "#E17474",
        dark:    "#9E2F2F",
      },
      neutral: {
        DEFAULT: "#C8D0DB", // muted
        light:   "#E5EAF1", // border/surface
        dark:    "#334155", // text
      },
    },
    effects: {
      glow:      "0 0 0 3px rgba(31, 63, 113, 0.25)",
      hoverGlow: "0 0 0 4px rgba(31, 63, 113, 0.20)",
    },
  },

  seedToSale: {
    name: "Seed to Sale",
    // A calm, agricultural palette—muted greens & slate neutrals.
    typography: {
      branding: 'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
      ui:       'Inter, system-ui, "Segoe UI", Roboto, Helvetica, Arial, sans-serif',
    },
    colors: {
      primary: { // Dominant Green
        DEFAULT: "#2E6B4D", 
        light:   "#3E8863", 
        dark:    "#1E4A35", 
      },
      secondary: { // Muted Plum/Purple
        DEFAULT: "#6E4D61",
        light:   "#8B637B",
        dark:    "#503846",
      },
      background: {
        DEFAULT: "#FFFFFF",
        light:   "#F6FAF7", 
        dark:    "#1A2421", 
      },
      accent: { // Golden Ochre/Yellow
        DEFAULT: "#D4A017",
        light:   "#EACD6F",
        dark:    "#A67C11",
      },
      danger: { // Rich Red
        DEFAULT: "#D94D4D",
        light:   "#E57373",
        dark:    "#B73E3E",
      },
      neutral: {
        DEFAULT: "#CAD6CF", 
        light:   "#E3ECE6", 
        dark:    "#2F3B36", 
      },
    },
    effects: {
      // Subtle rings aligned with green primary—no flashy glow.
      glow:      "0 0 0 3px rgba(30, 74, 53, 0.25)",
      hoverGlow: "0 0 0 4px rgba(30, 74, 53, 0.20)",
    },
  },
} as const

// --- Theme selection ---
// If you want env-controlled switching, uncomment the two lines below and
// ensure CLIENT_THEME is either "pharmaCore" or "seedToSale".
// const key = (import.meta as any)?.env?.CLIENT_THEME as keyof typeof themes
// const currentTheme = (key && themes[key]) ? themes[key] : themes.pharmaCore

export type ThemeKey = keyof typeof themes

export const defaultThemeKey: ThemeKey = 'seedToSale'

const currentTheme = themes[defaultThemeKey]

export default {
  themes,
  currentTheme,
  defaultThemeKey,
}
