<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex items-center gap-4 mb-6">
      <AppButton
        variant="tertiary"
        size="small"
        @click="navigateTo('/staff')"
      >
        ← Back to Staff
      </AppButton>
      <div>
        <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
          Add New Staff Member
        </h1>
        <p class="mt-1 text-sm text-neutral-dark/70 dark:text-neutral-light/70">
          Create a new staff member record
        </p>
      </div>
    </div>

    <!-- Form -->
    <div class="p-6 rounded-xl shadow-lg bg-background-light dark:bg-neutral-dark/60 border border-neutral-light/70 dark:border-neutral-dark/70">
      <DynamicCreateForm
        endpoint="/api/staff"
        :fields="formFields"
        title="Staff Information"
        subtitle="Fill in the details below"
        cache-invalidate-key="list:/api/staff"
        mode="create"
        @created="handleCreateSuccess"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
const { formFields } = useStaffColumns()

const handleCreateSuccess = () => {
  navigateTo('/staff')
}
</script>
