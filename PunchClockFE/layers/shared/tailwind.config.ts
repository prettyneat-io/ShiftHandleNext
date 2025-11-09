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
    '--theme-primary-alt': hexToRgb(theme.colors.primary.alt),
    '--theme-primary-light': hexToRgb(theme.colors.primary.light),
    '--theme-primary-dark': hexToRgb(theme.colors.primary.dark),
    '--theme-secondary': hexToRgb(theme.colors.secondary.DEFAULT),
    '--theme-secondary-light': hexToRgb(theme.colors.secondary.light),
    '--theme-secondary-dark': hexToRgb(theme.colors.secondary.dark),
    '--theme-background': hexToRgb(theme.colors.background.DEFAULT),
    '--theme-background-alt': hexToRgb(theme.colors.background.alt),
    '--theme-background-light': hexToRgb(theme.colors.background.light),
    '--theme-background-dark': hexToRgb(theme.colors.background.dark),
    '--theme-accent': hexToRgb(theme.colors.accent.DEFAULT),
    '--theme-accent-light': hexToRgb(theme.colors.accent.light),
    '--theme-accent-dark': hexToRgb(theme.colors.accent.dark),
    '--theme-danger': hexToRgb(theme.colors.danger.DEFAULT),
    '--theme-danger-light': hexToRgb(theme.colors.danger.light),
    '--theme-danger-dark': hexToRgb(theme.colors.danger.dark),
    '--theme-success': hexToRgb(theme.colors.success.DEFAULT),
    '--theme-success-light': hexToRgb(theme.colors.success.light),
    '--theme-success-dark': hexToRgb(theme.colors.success.dark),
    '--theme-warning': hexToRgb(theme.colors.warning.DEFAULT),
    '--theme-warning-light': hexToRgb(theme.colors.warning.light),
    '--theme-warning-dark': hexToRgb(theme.colors.warning.dark),
    '--theme-neutral': hexToRgb(theme.colors.neutral.DEFAULT),
    '--theme-neutral-light': hexToRgb(theme.colors.neutral.light),
    '--theme-neutral-mid': hexToRgb(theme.colors.neutral.mid),
    '--theme-neutral-dark': hexToRgb(theme.colors.neutral.dark),
    '--theme-panel-dark': hexToRgb(theme.colors.panel.dark),
    '--theme-text-dark': hexToRgb(theme.colors.text.dark),
    '--theme-text-dark-muted': hexToRgb(theme.colors.text.darkMuted),
    '--theme-border-dark': hexToRgb(theme.colors.border.dark),
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
          alt: withOpacityValue('--theme-primary-alt'),
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
          alt: withOpacityValue('--theme-background-alt'),
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
        success: {
          DEFAULT: withOpacityValue('--theme-success'),
          light: withOpacityValue('--theme-success-light'),
          dark: withOpacityValue('--theme-success-dark'),
        },
        warning: {
          DEFAULT: withOpacityValue('--theme-warning'),
          light: withOpacityValue('--theme-warning-light'),
          dark: withOpacityValue('--theme-warning-dark'),
        },
        neutral: {
          DEFAULT: withOpacityValue('--theme-neutral'),
          light: withOpacityValue('--theme-neutral-light'),
          mid: withOpacityValue('--theme-neutral-mid'),
          dark: withOpacityValue('--theme-neutral-dark'),
        },
        panel: {
          dark: withOpacityValue('--theme-panel-dark'),
        },
        text: {
          dark: withOpacityValue('--theme-text-dark'),
          darkMuted: withOpacityValue('--theme-text-dark-muted'),
        },
        border: {
          dark: withOpacityValue('--theme-border-dark'),
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
