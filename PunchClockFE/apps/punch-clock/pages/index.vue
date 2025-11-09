<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div>
      <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
        Dashboard
      </h1>
      <p class="mt-1 text-sm text-neutral-dark/70 dark:text-neutral-light/70">
        Overview of today's attendance and system status
      </p>
      <div v-if="error" class="mt-2 text-sm text-red-600 dark:text-red-400">
        {{ error }}
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <AppCard
        v-if="!loading"
        title="Total Staff"
        :subtitle="stats.totalStaff.toString()"
        to="/staff"
        initials="👥"
        bg-color="bg-primary/10 dark:bg-primary/20"
        wrapper-class="hover:shadow-xl"
      >
        <template #initials>
          <UsersIcon class="h-8 w-8 text-primary" />
        </template>
      </AppCard>
      <div v-else class="col-span-1 h-24 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>

      <AppCard
        v-if="!loading"
        title="Clocked In Today"
        :subtitle="stats.clockedIn.toString()"
        to="/attendance/live"
        initials="⏰"
        bg-color="bg-green-600/10 dark:bg-green-600/20"
        wrapper-class="hover:shadow-xl"
      >
        <template #initials>
          <ClockIcon class="h-8 w-8 text-green-600" />
        </template>
      </AppCard>
      <div v-else class="col-span-1 h-24 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>

      <AppCard
        v-if="!loading"
        title="Late Arrivals"
        :subtitle="stats.lateArrivals.toString()"
        to="/attendance"
        initials="⚠️"
        bg-color="bg-orange-600/10 dark:bg-orange-600/20"
        wrapper-class="hover:shadow-xl"
      >
        <template #initials>
          <ExclamationTriangleIcon class="h-8 w-8 text-orange-600" />
        </template>
      </AppCard>
      <div v-else class="col-span-1 h-24 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>

      <AppCard
        v-if="!loading"
        title="Pending Corrections"
        :subtitle="stats.pendingCorrections.toString()"
        to="/corrections"
        initials="📋"
        bg-color="bg-red-600/10 dark:bg-red-600/20"
        wrapper-class="hover:shadow-xl"
      >
        <template #initials>
          <DocumentCheckIcon class="h-8 w-8 text-red-600" />
        </template>
      </AppCard>
      <div v-else class="col-span-1 h-24 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
    </div>

    <!-- System Status -->
    <div class="p-6 rounded-xl shadow-lg bg-background-light dark:bg-neutral-dark/60 border border-neutral-light/70 dark:border-neutral-dark/70">
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light">
          System Status
        </h2>
        <div class="flex items-center space-x-2">
          <label class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">
            Auto-refresh
          </label>
          <input
            v-model="autoRefreshEnabled"
            type="checkbox"
            class="rounded border-neutral-light/70 dark:border-neutral-dark/70"
          />
        </div>
      </div>
      <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div class="flex items-center space-x-3">
          <div 
            class="h-3 w-3 rounded-full"
            :class="systemHealth?.status === 'healthy' || systemHealth?.status === 'Healthy' ? 'bg-green-500 animate-pulse' : 'bg-red-500'"
          ></div>
          <div>
            <p class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
              API Status
            </p>
            <p class="text-xs text-neutral-dark/70 dark:text-neutral-light/70">
              {{ systemHealth?.status || 'Checking...' }}
            </p>
          </div>
        </div>
        <div class="flex items-center space-x-3">
          <div 
            class="h-3 w-3 rounded-full"
            :class="stats.activeDevices > 0 ? 'bg-green-500 animate-pulse' : 'bg-orange-500'"
          ></div>
          <div>
            <p class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
              Active Devices
            </p>
            <p class="text-xs text-neutral-dark/70 dark:text-neutral-light/70">
              {{ stats.activeDevices }} / {{ stats.totalDevices }}
            </p>
          </div>
        </div>
        <div class="flex items-center space-x-3">
          <div class="h-3 w-3 rounded-full bg-green-500 animate-pulse"></div>
          <div>
            <p class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
              Last Sync
            </p>
            <p class="text-xs text-neutral-dark/70 dark:text-neutral-light/70">
              {{ stats.lastSync }}
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {
  UsersIcon,
  ClockIcon,
  ExclamationTriangleIcon,
  DocumentCheckIcon,
} from '@heroicons/vue/24/outline'

// Dashboard composable with live data
const { stats, loading, error, fetchDashboardStats, checkSystemHealth } = useDashboard()

// System health and auto-refresh
const systemHealth = ref<any>(null)
const autoRefreshEnabled = ref(true)

// Refresh dashboard data
const refreshData = async () => {
  await Promise.all([
    fetchDashboardStats(),
    checkSystemHealth().then(health => systemHealth.value = health)
  ])
}

// Auto-refresh setup
let refreshInterval: NodeJS.Timeout

onMounted(async () => {
  // Initial data load
  await refreshData()
  
  // Auto-refresh every 30 seconds if enabled
  refreshInterval = setInterval(() => {
    if (autoRefreshEnabled.value) {
      fetchDashboardStats()
    }
  }, 30000)
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
})
</script>
