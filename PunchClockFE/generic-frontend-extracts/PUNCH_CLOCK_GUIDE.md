# How to Use This Template for a Punch Clock Application

## Overview

This guide will help you adapt the generic frontend template for a time tracking/punch clock application.

## Step 1: Set Up Your Project

```bash
# Copy the extracted template to your new project
mkdir my-punch-clock-app
cd my-punch-clock-app
cp -r /path/to/generic-frontend-extracts/* .

# Rename the app
mv apps/app-template apps/punch-clock

# Install dependencies
pnpm install
```

## Step 2: Update Configuration

### Update `apps/punch-clock/package.json`
```json
{
  "name": "punch-clock-app",
  "version": "1.0.0"
}
```

### Update `apps/punch-clock/nuxt.config.ts`
Point to your backend API:
```typescript
runtimeConfig: {
  public: {
    apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:3002'
  }
}
```

## Step 3: Create Domain-Specific Data

### Create `layers/shared/data/time-tracking-endpoints.ts`
```typescript
export const TIME_ENDPOINTS: Record<string, string> = {
  'employees': 'employees',
  'time-entries': 'time-entries',
  'shifts': 'shifts',
  'departments': 'departments',
  'time-off-requests': 'time-off-requests',
  'payroll-periods': 'payroll-periods',
}
```

### Create `layers/shared/data/time-tracking-columns.ts`
Define table columns for your entities:
```typescript
export const TIME_COLUMNS = {
  'time-entries': [
    { key: 'id', label: 'ID' },
    { key: 'employee_name', label: 'Employee' },
    { key: 'clock_in', label: 'Clock In' },
    { key: 'clock_out', label: 'Clock Out' },
    { key: 'total_hours', label: 'Hours' },
    { key: 'status', label: 'Status' },
  ],
  'employees': [
    { key: 'id', label: 'ID' },
    { key: 'name', label: 'Name' },
    { key: 'department', label: 'Department' },
    { key: 'email', label: 'Email' },
    { key: 'status', label: 'Status' },
  ],
}
```

## Step 4: Create Your Pages

### Create `apps/punch-clock/pages/time-entries/index.vue`
```vue
<template>
  <div>
    <AppHeader title="Time Entries" />
    <ListTemplate
      :endpoint="TIME_ENDPOINTS['time-entries']"
      :columns="TIME_COLUMNS['time-entries']"
      entity-name="Time Entry"
    />
  </div>
</template>

<script setup>
import { TIME_ENDPOINTS, TIME_COLUMNS } from '~/data/time-tracking-endpoints'
</script>
```

### Create `apps/punch-clock/pages/clock.vue`
A custom page for clocking in/out:
```vue
<template>
  <div class="max-w-md mx-auto p-6">
    <AppCard>
      <h2 class="text-2xl font-bold mb-4">Punch Clock</h2>
      
      <div v-if="currentEntry" class="mb-4">
        <p>Currently clocked in at: {{ formatTime(currentEntry.clock_in) }}</p>
        <AppButton @click="clockOut" class="w-full mt-4">
          Clock Out
        </AppButton>
      </div>
      
      <div v-else>
        <AppButton @click="clockIn" class="w-full">
          Clock In
        </AppButton>
      </div>
    </AppCard>
  </div>
</template>

<script setup>
const { $api } = useNuxtApp()
const currentEntry = ref(null)

async function clockIn() {
  const response = await $api('/time-entries/clock-in', { method: 'POST' })
  currentEntry.value = response.data
}

async function clockOut() {
  await $api(`/time-entries/${currentEntry.value.id}/clock-out`, { 
    method: 'PATCH' 
  })
  currentEntry.value = null
}

function formatTime(time) {
  return new Date(time).toLocaleTimeString()
}
</script>
```

## Step 5: Update Sidebar Navigation

Update the sidebar in your app to show punch clock specific navigation:

### Create/Update `apps/punch-clock/app.vue`
Override the sidebar items:
```vue
<template>
  <div class="h-full min-h-screen">
    <NuxtLayout>
      <NuxtPage />
    </NuxtLayout>
  </div>
</template>

<script setup>
// Define punch clock specific navigation
const sidebarItems = [
  { name: 'Dashboard', path: '/', icon: 'home' },
  { name: 'Clock In/Out', path: '/clock', icon: 'clock' },
  { name: 'Time Entries', path: '/time-entries', icon: 'list' },
  { name: 'Employees', path: '/employees', icon: 'users' },
  { name: 'Reports', path: '/reports', icon: 'chart' },
]

provide('sidebarItems', sidebarItems)
</script>
```

## Step 6: Leverage Existing Features

The template already includes:

✅ **Authentication** - Login/logout functionality ready to use
✅ **Form System** - Use `AppForm` for employee forms, time-off requests, etc.
✅ **List Views** - Use `ListTemplate` for displaying tables of data
✅ **Export** - Built-in CSV export via `useExportData`
✅ **Notifications** - Show success/error messages
✅ **Loading States** - Global loading indicator
✅ **Responsive Design** - Mobile-friendly out of the box

## Step 7: Run Your Application

```bash
cd apps/punch-clock
pnpm dev
```

Visit http://localhost:3000

## Customization Tips

1. **Theme**: Modify `layers/shared/tailwind.config.ts` for colors and styling
2. **Components**: Create domain-specific components in `apps/punch-clock/components/`
3. **Types**: Add TypeScript types in `layers/shared/types/`
4. **API**: The `$api` plugin is ready to use for all backend calls
5. **Validation**: Use the validation schema generator for forms

## Example: Creating a Quick Report Page

```vue
<template>
  <div class="p-6">
    <AppHeader title="Time Reports" />
    
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
      <OverviewCard
        title="Total Hours Today"
        :value="stats.todayHours"
        icon="clock"
      />
      <OverviewCard
        title="Employees Clocked In"
        :value="stats.clockedIn"
        icon="users"
      />
      <OverviewCard
        title="This Week"
        :value="stats.weekHours"
        icon="calendar"
      />
    </div>
    
    <AppCard>
      <h3 class="text-lg font-semibold mb-4">Recent Entries</h3>
      <!-- Use existing ListTemplate or custom table -->
    </AppCard>
  </div>
</template>

<script setup>
const { $api } = useNuxtApp()
const stats = ref({})

onMounted(async () => {
  const response = await $api('/time-entries/stats')
  stats.value = response.data
})
</script>
```

## Next Steps

1. Set up your backend API with matching endpoints
2. Create Zod or Yup schemas for form validation
3. Add role-based permissions if needed
4. Customize the theme to match your brand
5. Add more domain-specific features

Happy coding! 🚀
