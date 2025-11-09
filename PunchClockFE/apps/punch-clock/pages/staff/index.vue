<template>
  <ListViewPage
    title="Staff Directory"
    subtitle="Manage and view all staff members"
    endpoint="staff"
    search-column="firstName"
    :columns="listColumns"
    create-route="/staff/new"
  />
</template>

<script setup lang="ts">
const { listColumns } = useStaffColumns()
</script>

