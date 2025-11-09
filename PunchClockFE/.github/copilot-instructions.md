# ShiftHandle Next - AI Assistant Guide

## Stack
Nuxt 3 monorepo: `layers/shared/` (reusable) + `apps/punch-clock/` (main app). TypeScript, Pinia, VeeValidate, Tailwind. Backend: .NET API at `localhost:5187`.

## Critical Rules

1. **API Client**: NEVER edit `apps/punch-clock/lib/punch-clock-api.ts` (auto-generated). Use `usePunchClockApi()` composable.
2. **Components**: Shared = `App*` prefix in `layers/shared/components/`. App-specific = no prefix in `apps/punch-clock/components/`.
3. **Layers**: Check `layers/shared/` first before creating new code. Reusable → shared, specific → app.
4. **Forms**: Use `DynamicCreateForm` + schemas in `useFormSchemas.ts` + `validation-meta.json`.
5. **Types**: Everything typed. No `any`. Props, emits, API responses all typed.

## Key Files
- **API**: `apps/punch-clock/lib/punch-clock-api.ts` (generated), `composables/usePunchClockApi.ts`
- **Stores**: `layers/shared/stores/{auth,loading,notification,formCache}.ts`
- **Forms**: `layers/shared/composables/useFormSchemas.ts`, `utils/validation-meta.json`
- **Auth**: `apps/punch-clock/middleware/auth.global.ts` (protects all routes except login/signup/forgot-password)

## Patterns

**API Call**
```ts
const api = usePunchClockApi()
const { showNotification } = useNotificationStore()
try {
  const result = await api.method()
  showNotification('Success', 'success')
} catch (error) {
  showNotification('Error', 'error')
}
```

**Component**
```vue
<script setup lang="ts">
interface Props { title: string }
const props = defineProps<Props>()
const emit = defineEmits<{ close: [] }>()
</script>
```

**Composable**
```ts
export const useFeature = () => {
  const data = ref<Type | null>(null)
  const load = async () => { /* ... */ }
  return { data: readonly(data), load }
}
```

## Commands
```bash
pnpm install      # Root only
pnpm run dev      # Start dev server
pnpm run build    # Production build
pnpm run clean    # Remove node_modules/.nuxt/.output
```

## Common Tasks
- **New page**: `apps/punch-clock/pages/name.vue`, add to `config/navigation.ts` if needed
- **New component**: Determine shared vs app-specific, follow naming, use TypeScript
- **New form**: Use `DynamicCreateForm` or add schema to `useFormSchemas.ts`
- **New store**: `defineStore` with setup syntax, export composable

## DO / DON'T
✅ Type everything, handle errors/loading, use existing shared components, Tailwind classes
❌ Edit generated API client, skip types, ignore errors, hardcode endpoints, inline styles

## Frontend Style Guide
- **Architecture**: Honour layer split; shared logic/components live in `layers/shared`, app-specific pieces stay in `apps/punch-clock`. Never duplicate what already exists.
- **Type Safety**: No `any`, always define interfaces or types near usage; wire API responses through typed composables or stores.
- **State & Data**: Centralise long-lived state in Pinia stores; keep component state local and minimal. All API calls go through `usePunchClockApi()` inside composables/services.
- **Components**: Prefer script setup + `<template>` single responsibility. Extract form logic into `DynamicCreateForm` and schemas; emit events instead of reaching into parents.
- **Styling**: Tailwind for layout/spacing; avoid inline styles or magic numbers. Respect dark mode classes already in theme.
- **Error & Loading UX**: Surface errors via `useNotificationStore()`; show skeletons/spinners while fetching; never leave users guessing.
- **Testing & QA**: Smoke critical flows after changes; add lightweight unit tests for composables/stores when logic grows; validate forms against schema updates.

## Troubleshooting
- Types missing → check imports and generated client
- 404 errors → verify Vite proxy and `BACKEND_URL`
- Auth issues → clear localStorage, check token
- Component not found → verify layer extends

When implementing: Check existing code → Use proper layer → Type everything → Handle errors/loading → Test responsively
