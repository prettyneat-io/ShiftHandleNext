<template>
  <div class="space-y-6">
    <!-- Breadcrumbs -->
    <AppNestedBreadcrumbs
      :trail="[
        { name: 'Staff Directory' },
        { name: staffName },
        { name: 'Edit' }
      ]"
    />

    <!-- Loading State -->
    <div v-if="loading" class="p-6 rounded-xl shadow-lg bg-background-light dark:bg-neutral-dark/60 border border-neutral-light/70 dark:border-neutral-dark/70">
      <div class="h-96 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
    </div>

    <!-- Error State -->
    <AppCard v-else-if="error">
      <div class="text-center py-8">
        <p class="text-red-600 dark:text-red-400">{{ error }}</p>
        <AppButton variant="primary" size="small" class="mt-4" @click="fetchStaffDetail">
          Retry
        </AppButton>
      </div>
    </AppCard>

    <!-- Form -->
    <div v-else-if="staff" class="p-6 rounded-xl shadow-lg bg-background-light dark:bg-neutral-dark/60 border border-neutral-light/70 dark:border-neutral-dark/70">
      <DynamicCreateForm
        :endpoint="`/api/staff`"
        :fields="formFields"
        title="Edit Staff Information"
        subtitle="Update the details below"
        :cache-invalidate-key="`list:/api/staff`"
        mode="edit"
        :record-id="staffId"
        :initial-data="staff"
        @updated="handleUpdateSuccess"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import type { Staff } from '~/lib/punch-clock-api'

const route = useRoute()
const api = usePunchClockApi()
const notificationStore = useNotificationStore()
const { formFields } = useStaffColumns()

// Route params
const staffId = computed(() => route.params.id as string)

// State
const staff = ref<Staff | null>(null)
const loading = ref(false)
const error = ref<string | null>(null)

// Computed
const staffName = computed(() => {
  if (!staff.value) return 'Loading...'
  return `${staff.value.firstName} ${staff.value.lastName}`
})

// Fetch staff detail
const fetchStaffDetail = async () => {
  loading.value = true
  error.value = null
  try {
    const response = await api.getStaffByIdStaff(staffId.value)
    staff.value = (response as any)?.data || response as any
  } catch (err: any) {
    error.value = err.message || 'Failed to load staff details'
    notificationStore.addNotification({ message: 'Failed to load staff details', type: 'error' })
  } finally {
    loading.value = false
  }
}

const handleUpdateSuccess = () => {
  notificationStore.addNotification({ message: 'Staff updated successfully', type: 'success' })
  navigateTo(`/staff/${staffId.value}`)
}

// Initialize
onMounted(() => {
  fetchStaffDetail()
})
</script>