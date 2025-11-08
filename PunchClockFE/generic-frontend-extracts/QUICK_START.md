# Quick Start Guide

## For Punch Clock Application

```bash
# 1. Copy template to new project
cp -r generic-frontend-extracts/ ~/my-punch-clock-app/

# 2. Navigate and rename
cd ~/my-punch-clock-app/
mv apps/app-template apps/punch-clock

# 3. Install dependencies
pnpm install

# 4. Start development server
cd apps/punch-clock
pnpm dev
```

## Create Your First Page

### 1. Define Endpoints
Create `layers/shared/data/punch-clock-endpoints.ts`:

```typescript
export const ENDPOINTS = {
  'employees': 'employees',
  'time-entries': 'time-entries',
  'shifts': 'shifts',
}
```

### 2. Create a Page
Create `apps/punch-clock/pages/employees/index.vue`:

```vue
<template>
  <div>
    <AppHeader title="Employees" />
    <ListTemplate
      endpoint="employees"
      entity-name="Employee"
    />
  </div>
</template>
```

### 3. Run Your App
```bash
pnpm dev
```

Visit: http://localhost:3000

## Available Components

- `<AppButton>` - Buttons with variants
- `<AppCard>` - Card containers
- `<AppModal>` - Modal dialogs
- `<AppForm>` - Dynamic forms
- `<AppInput>` - Form inputs
- `<ListTemplate>` - List views with CRUD
- And more...

## Available Stores

```typescript
import { useAuthStore } from '@/stores/auth'
import { useNotificationStore } from '@/stores/notification'
import { useLoadingStore } from '@/stores/loading'

const authStore = useAuthStore()
const notifications = useNotificationStore()
const loading = useLoadingStore()
```

## API Calls

```typescript
const { $api } = useNuxtApp()

// GET request
const data = await $api('/employees')

// POST request
await $api('/employees', {
  method: 'POST',
  body: { name: 'John Doe' }
})
```

## Documentation

- `README.md` - Full overview
- `PUNCH_CLOCK_GUIDE.md` - Detailed step-by-step guide
- `EXTRACTION_SUMMARY.md` - What's included

## Support

All components are TypeScript-ready and fully documented with JSDoc comments.
