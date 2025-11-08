import themeConfig from './theme.config'

const theme = themeConfig.currentTheme

const withOpacityValue = (variable: string) => ({ opacityValue }: { opacityValue?: string }) => {
  if (opacityValue !== undefined) {
    return `rgb(var(${variable}) / ${opacityValue})`
  }
  return `rgb(var(${variable}))`
}

const hexToRgb = (hex: string) => {
  const sanitized = hex.replace('#', '')
  const bigint = parseInt(sanitized, 16)
  const r = (bigint >> 16) & 255
  const g = (bigint >> 8) & 255
  const b = bigint & 255
  return `${r} ${g} ${b}`
}

const baseThemeVariables = {
  ':root': {
    '--theme-primary': hexToRgb(theme.colors.primary.DEFAULT),
    '--theme-primary-light': hexToRgb(theme.colors.primary.light),
    '--theme-primary-dark': hexToRgb(theme.colors.primary.dark),
    '--theme-secondary': hexToRgb(theme.colors.secondary.DEFAULT),
    '--theme-secondary-light': hexToRgb(theme.colors.secondary.light),
    '--theme-secondary-dark': hexToRgb(theme.colors.secondary.dark),
    '--theme-background': hexToRgb(theme.colors.background.DEFAULT),
    '--theme-background-light': hexToRgb(theme.colors.background.light),
    '--theme-background-dark': hexToRgb(theme.colors.background.dark),
    '--theme-accent': hexToRgb(theme.colors.accent.DEFAULT),
    '--theme-accent-light': hexToRgb(theme.colors.accent.light),
    '--theme-accent-dark': hexToRgb(theme.colors.accent.dark),
    '--theme-danger': hexToRgb(theme.colors.danger.DEFAULT),
    '--theme-danger-light': hexToRgb(theme.colors.danger.light),
    '--theme-danger-dark': hexToRgb(theme.colors.danger.dark),
    '--theme-neutral': hexToRgb(theme.colors.neutral.DEFAULT),
    '--theme-neutral-light': hexToRgb(theme.colors.neutral.light),
    '--theme-neutral-dark': hexToRgb(theme.colors.neutral.dark),
    '--theme-shadow-glow': theme.effects.glow,
    '--theme-shadow-hover': theme.effects.hoverGlow,
  },
}

type ClassUtilities = {
  addUtilities: (utilities: object, variants?: string[]) => void;
};

export default {
  darkMode: 'class',
  content: [
    './pages/**/*.{js,ts,vue}',
    '../../apps/ab-distribution/pages/**/*.{vue,js,ts}',
    '../../apps/ab-distribution/components/**/*.{vue,js,ts}',
    '../../apps/ab-cultivation/pages/**/*.{vue,js,ts}',
    '../../apps/ab-cultivation/components/**/*.{vue,js,ts}',
    '../../layers/shared/components/**/*.{vue,js,ts}',
    '../../layers/shared/layouts/**/*.{vue,js,ts}',
    '../../node_modules/@tailwindplus/elements/**/*.{js,ts,vue}',
    '../../../node_modules/@tailwindplus/elements/**/*.{js,ts,vue}',
    'node_modules/@tailwindplus/elements/**/*.{js,ts,vue}'
  ],
  theme: {
    extend: {
      fontFamily: {
        branding: [theme.typography.branding],
        ui: [theme.typography.ui]
      },
      colors: {
        primary: {
          DEFAULT: withOpacityValue('--theme-primary'),
          light: withOpacityValue('--theme-primary-light'),
          dark: withOpacityValue('--theme-primary-dark'),
        },
        secondary: {
          DEFAULT: withOpacityValue('--theme-secondary'),
          light: withOpacityValue('--theme-secondary-light'),
          dark: withOpacityValue('--theme-secondary-dark'),
        },
        background: {
          DEFAULT: withOpacityValue('--theme-background'),
          light: withOpacityValue('--theme-background-light'),
          dark: withOpacityValue('--theme-background-dark'),
        },
        accent: {
          DEFAULT: withOpacityValue('--theme-accent'),
          light: withOpacityValue('--theme-accent-light'),
          dark: withOpacityValue('--theme-accent-dark'),
        },
        danger: {
          DEFAULT: withOpacityValue('--theme-danger'),
          light: withOpacityValue('--theme-danger-light'),
          dark: withOpacityValue('--theme-danger-dark'),
        },
        neutral: {
          DEFAULT: withOpacityValue('--theme-neutral'),
          light: withOpacityValue('--theme-neutral-light'),
          dark: withOpacityValue('--theme-neutral-dark'),
        }
      },
      boxShadow: {
        glow: 'var(--theme-shadow-glow)',
        hoverGlow: 'var(--theme-shadow-hover)'
      }
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
    function ({ addBase }: { addBase: (base: Record<string, Record<string, string>>) => void }) {
      addBase(baseThemeVariables)
    },
    function ({ addUtilities }: ClassUtilities) {
      addUtilities({
        '.hoverGlow': {
          boxShadow: 'var(--theme-shadow-hover)',
        }
      }, ['responsive', 'hover'])
    }
  ]
};
