# ShiftHandle Next Frontend Style Guide

Updated: 9 November 2025

ShiftHandle Next is a precision workforce orchestration platform. The interface should feel dependable, rhythmic, and purpose-built for tracking time and keeping teams synchronized. This guide defines the visual and interaction system for all future development.

---

## Design Intent

- Communicate accuracy, trust, and operational calm.
- Present data clearly and consistently across light and dark modes.
- Provide quick legibility for time metrics, shift status, and attendance alerts.

---

## Color System

### Core Palette

| Token | Hex | Usage |
| --- | --- | --- |
| `primary` | #14647A | Main actions, highlights, links |
| `primary-alt` | #1F7CA8 | Hover states, secondary emphasis |
| `secondary` | #65708A | Secondary buttons, tabs, muted icons |
| `background` | #F5F7FA | Page surface |
| `background-alt` | #E6EAEE | Alternating rows, card fills |
| `neutral-dark` | #273141 | Primary text |
| `neutral-mid` | #4D596A | Labels, icons |
| `neutral-light` | #C6CED8 | Borders, separators |
| `success` | #1F8A56 | Healthy status, clocked in |
| `warning` | #D2951C | Late arrivals, pending review |
| `danger-accent` | #B73131 | Destructive icons/text only |
| `info` | #3C73D3 | Info banners, analytics cards |

Purple and saturated red must not be used for base surfaces or brand elements. Reserve `danger-accent` for alerts and destructive actions only.

### Dark Mode Tokens

| Token | Hex | Usage |
| --- | --- | --- |
| `background-dark` | #111820 | Primary dark surface |
| `panel-dark` | #1A2330 | Cards, modals |
| `text-dark` | #F1F5F9 | Primary text |
| `text-dark-muted` | #CBD5E1 | Secondary text |
| `border-dark` | #2E3A4A | Borders |

---

## Typography

- Primary font: Inter (retain existing stack).
- Use `tabular-nums` for timestamps, durations, and counters.
- Page title: `text-3xl font-semibold tracking-tight text-neutral-dark`.
- Section title: `text-xl font-semibold text-neutral-dark/90`.
- Subheading: `text-sm font-medium tracking-wide uppercase text-neutral-mid/80`.
- Body: `text-sm text-neutral-mid` (light mode) and `text-dark-muted` (dark mode).

---

## Spacing & Layout

- Global vertical rhythm: `space-y-6` (24px) between major blocks.
- Stat tiles: `grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5`.
- Panels: `p-6` padding with consistent gutters.
- Maintain consistent baseline through use of Tailwind spacing scale (`4`, `6`, `8`, `12`).

---

## Border Radius

| Element | Class | Notes |
| --- | --- | --- |
| Buttons & Inputs | `rounded-md` | Approx. 8px |
| Cards & Modals | `rounded-lg` | Approx. 12px |
| Chips & Pills | `rounded-full` | Full radius |
| Tables | `rounded-md` | Header/footer alignment |

---

## Elevation & Shadows

- Resting cards: `shadow-sm shadow-neutral-dark/10`.
- Hover cards: `shadow-xl shadow-primary/10`.
- Primary buttons: `shadow-md shadow-primary/15` on hover with gradient sheen animation (`bg-gradient-to-r` shifting toward `primary-alt`).
- Modals: `shadow-2xl shadow-neutral-dark/20` with subtle drop.
- Avoid heavy blur shadows; keep crisp for clarity.

---

## Buttons

Update `AppButton` variants to match the palette.

| Variant | Base Styles | Hover | Focus |
| --- | --- | --- | --- |
| Primary | `bg-gradient-to-r from-primary to-primary-alt text-white` | `from-primary-alt to-primary-alt/80` | `ring-2 ring-primary-alt/40 ring-offset-2` |
| Secondary | `bg-gradient-to-r from-white to-background-alt text-primary border border-primary/20` | `border-primary text-primary-alt` | `ring-2 ring-primary/30` |
| Tertiary | `bg-transparent text-primary underline-offset-4` | `text-primary-alt underline` | `ring-2 ring-primary/30` |
| Destructive | `bg-transparent text-danger-accent border border-danger-accent/40` | `bg-danger-accent/10` | `ring-2 ring-danger-accent/40` |
| Disabled | `opacity-50 cursor-not-allowed` | N/A | N/A |

Include loading states with teal spinner (`text-primary-alt`).

---

## Inputs & Forms

- Fields: `rounded-md border border-neutral-light/70 bg-white focus:border-primary focus:ring-2 focus:ring-primary/30`.
- Read-only: `bg-background-alt/60 text-neutral-dark`.
- Error state: `border-[#BF4141] bg-[#BF4141]/5 text-[#BF4141]` with message `text-sm text-[#BF4141]`.
- Labels: `text-sm font-medium tracking-wide uppercase text-neutral-mid/80`.
- Helper text: `text-xs text-neutral-mid/70`.
- Group forms with `space-y-6` for readability.

---

## Cards & Metrics

- Card surface: `rounded-lg bg-white border border-neutral-light/60 shadow-lg shadow-neutral-dark/5`.
- Add subtle gradient strip: `bg-gradient-to-br from-background-alt to-white` when highlighting metrics.
- Metric header: `text-xs uppercase tracking-wide text-secondary/80`.
- Metric value: `text-3xl font-semibold tabular-nums text-neutral-dark`.
- Trend pill: `rounded-full px-2 py-1 text-xs text-primary bg-primary/10`.

---

## Navigation

- Header: `bg-white/95 border-b border-neutral-light/60 backdrop-blur`.
- Active top nav: `text-primary border-b-2 border-primary`.
- Hover: `text-primary-alt`.
- Sidebar (if used): `bg-neutral-dark text-white`, active item `bg-primary/20 border-l-4 border-primary text-white`.
- Include icons with `text-secondary/80` baseline, `text-primary` for active.

---

## Status Indicators

- Healthy: `bg-success animate-pulse`.
- Degraded: `bg-warning animate-ping`.
- Offline: `bg-neutral-light ring-2 ring-danger-accent/40`.
- Provide tooltips for status dots and metrics.

---

## Dark Mode Guidelines

- Background: `bg-background-dark`.
- Panels: `bg-panel-dark border border-border-dark`.
- Primary text: `text-text-dark`; secondary: `text-text-dark-muted`.
- Primary button: `bg-primary-alt/80 hover:bg-primary-alt focus:ring-primary-alt/50`.
- Inputs: `bg-white/5 border border-border-dark focus:border-primary focus:ring-2 focus:ring-primary/40`.
- Shadows: `shadow-black/60` with subtle offsets.
- Maintain color temperature consistency for teal and blue tones.

---

## Micro-interactions & Motion

- Hover transitions: `transition-all duration-180 ease-out`.
- Exit transitions: `transition-all duration-120 ease-in`.
- Modal entry: drop from top (`translate-y-2`) with opacity fade.
- Live metrics: soft pulse `animate-[pulse_2s_ease-in-out_infinite]` at low opacity.
- Skeleton loaders: `animate-pulse bg-gradient-to-r from-background-alt/40 via-white/60 to-background-alt/40`.

---

## Iconography

- Use Heroicons outline set for consistency.
- Introduce clock-hand, shift-grid, badge glyphs where custom icons required.
- Icon color defaults: `text-secondary` or `text-neutral-mid`; active states `text-primary`.
- Icon sizes: `h-5 w-5` (standard), `h-6 w-6` (prominent), `h-8 w-8` (hero).

---

## Tables & Lists

- Headers: `uppercase text-xs text-secondary/70 bg-background-alt tracking-wide`.
- Rows: zebra striping `odd:bg-white even:bg-background-alt/60`.
- Hover row: `bg-primary/5`.
- Selected row: `border-l-4 border-primary bg-primary/10`.
- Totals row: `bg-background-alt font-semibold`.

---

## Alerts & Notifications

| Type | Base Styles |
| --- | --- |
| Success | `bg-success/15 text-success border border-success/40` |
| Info | `bg-info/10 text-info border border-info/30` |
| Warning | `bg-warning/15 text-warning border border-warning/40` |
| Error | `bg-[#BF4141]/10 text-[#BF4141] border border-[#BF4141]/40` |

Close buttons use `text-neutral-mid hover:text-neutral-dark`.

---

## Accessibility

- Maintain minimum contrast ratio of 4.5:1 for text.
- Provide keyboard focus via `ring-2 ring-offset-2 ring-primary/50`.
- Ensure logical tab order and Escape support for modals.
- Use `tabular-nums` and `aria-label` for time displays.

---

## Implementation Checklist

1. Update `layers/shared/theme.config.ts` with new token values and names.
2. Sync Tailwind configuration to expose tokens as CSS variables.
3. Refactor `AppButton`, `AppInput`, `AppCard`, `AppHeader`, and `AppSidebar` to use updated tokens and styles.
4. Audit pages for hardcoded colors (remove purples and saturated reds).
5. Apply new typography hierarchy and spacing system across layouts.
6. Revisit status indicators, alerts, and notifications with new colors.
7. Validate light and dark modes for consistency and contrast.

---

## References & Inspiration

- Clean time management UI (e.g., Tracking.im) for calibration and tone.
- Industrial instrumentation for rhythm and precision motifs.

This guide is the source of truth for frontend visual and interaction decisions. Revisit and extend as new components emerge.
