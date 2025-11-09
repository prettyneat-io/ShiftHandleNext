<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
          Device Dashboard
        </h1>
        <p class="mt-1 text-sm text-neutral-dark/70 dark:text-neutral-light/70">
          Monitor and manage all biometric devices
        </p>
      </div>
      <div class="flex flex-wrap gap-3">
        <AppButton
          variant="secondary"
          size="small"
          @click="refreshDevices"
        >
          Refresh
        </AppButton>
        <AppButton
          variant="primary"
          size="small"
          @click="navigateTo('/devices/new')"
        >
          Add Device
        </AppButton>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 md:grid-cols-4 gap-6">
      <AppCard title="Total Devices" :subtitle="stats.total.toString()">
        <template #initials>
          <ComputerDesktopIcon class="h-8 w-8 text-primary" />
        </template>
      </AppCard>
      <AppCard title="Online" :subtitle="stats.online.toString()">
        <template #initials>
          <CheckCircleIcon class="h-8 w-8 text-green-600" />
        </template>
      </AppCard>
      <AppCard title="Offline" :subtitle="stats.offline.toString()">
        <template #initials>
          <XCircleIcon class="h-8 w-8 text-red-600" />
        </template>
      </AppCard>
      <AppCard title="Syncing" :subtitle="stats.syncing.toString()">
        <template #initials>
          <ArrowPathIcon class="h-8 w-8 text-blue-600" />
        </template>
      </AppCard>
    </div>

    <!-- Filters -->
    <AppCard>
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <AppInput
          v-model="filters.search"
          placeholder="Search devices..."
          @update:model-value="debouncedSearch"
        />
        <select
          v-model="filters.location"
          class="rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
          @change="fetchDevices"
        >
          <option value="">All Locations</option>
          <option v-for="loc in locations" :key="loc.locationId" :value="loc.locationId">
            {{ loc.name }}
          </option>
        </select>
        <select
          v-model="filters.status"
          class="rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
          @change="fetchDevices"
        >
          <option value="">All Status</option>
          <option value="online">Online</option>
          <option value="offline">Offline</option>
          <option value="syncing">Syncing</option>
        </select>
        <div class="flex items-center gap-2">
          <label class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">View:</label>
          <button
            :class="[
              'px-3 py-1 rounded-md transition-colors text-sm',
              viewMode === 'grid'
                ? 'bg-primary text-white'
                : 'bg-neutral-light/30 dark:bg-neutral-dark/30'
            ]"
            @click="viewMode = 'grid'"
          >
            Grid
          </button>
          <button
            :class="[
              'px-3 py-1 rounded-md transition-colors text-sm',
              viewMode === 'map'
                ? 'bg-primary text-white'
                : 'bg-neutral-light/30 dark:bg-neutral-dark/30'
            ]"
            @click="viewMode = 'map'"
          >
            Map
          </button>
        </div>
      </div>
    </AppCard>

    <!-- Loading State -->
    <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div v-for="i in 6" :key="i" class="h-64 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
    </div>

    <!-- Error State -->
    <AppCard v-else-if="error">
      <div class="text-center py-8">
        <p class="text-red-600 dark:text-red-400">{{ error }}</p>
        <AppButton variant="primary" size="small" class="mt-4" @click="fetchDevices">
          Retry
        </AppButton>
      </div>
    </AppCard>

    <!-- Grid View -->
    <div v-else-if="viewMode === 'grid'" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <AppCard
        v-for="device in devices"
        :key="device.deviceId"
        class="relative group hover:shadow-xl transition-shadow cursor-pointer"
        @click="navigateTo(`/devices/${device.deviceId}`)"
      >
        <!-- Status Indicator -->
        <div class="absolute top-4 right-4">
          <div
            :class="[
              'h-3 w-3 rounded-full',
              getDeviceStatusColor(device.status)
            ]"
          ></div>
        </div>

        <div class="space-y-4">
          <!-- Device Icon & Name -->
          <div class="flex items-center gap-4">
            <div class="h-16 w-16 rounded-lg bg-primary/10 flex items-center justify-center">
              <ComputerDesktopIcon class="h-8 w-8 text-primary" />
            </div>
            <div class="flex-1">
              <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
                {{ device.name }}
              </h3>
              <p class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">
                {{ device.serialNumber }}
              </p>
            </div>
          </div>

          <!-- Device Details -->
          <div class="space-y-2 text-sm">
            <div class="flex items-center justify-between">
              <span class="text-neutral-dark/60 dark:text-neutral-light/60">Location</span>
              <span class="text-neutral-dark dark:text-neutral-light font-medium">
                {{ device.location?.name || '—' }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-neutral-dark/60 dark:text-neutral-light/60">IP Address</span>
              <span class="text-neutral-dark dark:text-neutral-light font-mono text-xs">
                {{ device.ipAddress || '—' }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-neutral-dark/60 dark:text-neutral-light/60">Enrolled Users</span>
              <span class="text-neutral-dark dark:text-neutral-light font-medium">
                {{ device.enrolledUsersCount || 0 }}
              </span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-neutral-dark/60 dark:text-neutral-light/60">Last Sync</span>
              <span class="text-neutral-dark dark:text-neutral-light text-xs">
                {{ formatRelativeTime(device.lastSyncDate) }}
              </span>
            </div>
          </div>

          <!-- Status Badge -->
          <div class="flex items-center gap-2">
            <span
              :class="[
                'px-3 py-1 text-xs rounded-full font-medium',
                getDeviceStatusBadgeClass(device.status)
              ]"
            >
              {{ device.status || 'Unknown' }}
            </span>
            <span
              v-if="device.isActive"
              class="px-3 py-1 text-xs rounded-full font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
            >
              Active
            </span>
          </div>

          <!-- Quick Actions -->
          <div class="flex gap-2 pt-2 border-t border-neutral-light/50 dark:border-neutral-dark/50" @click.stop>
            <AppButton
              variant="tertiary"
              size="small"
              class="flex-1"
              @click="syncDevice(device.deviceId!)"
            >
              Sync
            </AppButton>
            <AppButton
              variant="tertiary"
              size="small"
              class="flex-1"
              @click="testConnection(device.deviceId!)"
            >
              Test
            </AppButton>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Map View Placeholder -->
    <AppCard v-else>
      <div class="text-center py-16">
        <MapIcon class="h-16 w-16 text-neutral-dark/20 dark:text-neutral-light/20 mx-auto mb-4" />
        <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light mb-2">
          Map View Coming Soon
        </h3>
        <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">
          Geographic device distribution will be available in a future update
        </p>
      </div>
    </AppCard>

    <!-- Pagination -->
    <div v-if="!loading && totalPages > 1" class="flex items-center justify-between">
      <p class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">
        Showing {{ (currentPage - 1) * pageSize + 1 }} to {{ Math.min(currentPage * pageSize, totalRecords) }} of {{ totalRecords }} devices
      </p>
      <div class="flex gap-2">
        <AppButton
          variant="secondary"
          size="small"
          :disabled="currentPage === 1"
          @click="goToPage(currentPage - 1)"
        >
          Previous
        </AppButton>
        <AppButton
          v-for="page in visiblePages"
          :key="page"
          :variant="page === currentPage ? 'primary' : 'tertiary'"
          size="small"
          @click="goToPage(page)"
        >
          {{ page }}
        </AppButton>
        <AppButton
          variant="secondary"
          size="small"
          :disabled="currentPage === totalPages"
          @click="goToPage(currentPage + 1)"
        >
          Next
        </AppButton>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {
  ComputerDesktopIcon,
  CheckCircleIcon,
  XCircleIcon,
  ArrowPathIcon,
  MapIcon
} from '@heroicons/vue/24/outline'
import type { Device } from '~/lib/punch-clock-api'

const api = usePunchClockApi()
const { showNotification } = useNotificationStore()

// State
const devices = ref<Device[]>([])
const locations = ref<any[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const viewMode = ref<'grid' | 'map'>('grid')

// Filters
const filters = ref({
  search: '',
  location: '',
  status: ''
})

// Pagination
const currentPage = ref(1)
const pageSize = ref(12)
const totalRecords = ref(0)
const totalPages = computed(() => Math.ceil(totalRecords.value / pageSize.value))

// Stats
const stats = computed(() => {
  return {
    total: devices.value.length,
    online: devices.value.filter(d => d.status?.toLowerCase() === 'online').length,
    offline: devices.value.filter(d => d.status?.toLowerCase() === 'offline').length,
    syncing: devices.value.filter(d => d.status?.toLowerCase() === 'syncing').length
  }
})

// Fetch devices
const fetchDevices = async () => {
  loading.value = true
  error.value = null
  try {
    const isActive = filters.value.status === 'online' ? true : filters.value.status === 'offline' ? false : undefined
    
    const response = await api.getAllDevicesDevices(
      currentPage.value,
      pageSize.value,
      'name',
      'asc',
      'location',
      isActive
    )
    
    devices.value = (response as any)?.data || []
    totalRecords.value = (response as any)?.total || devices.value.length
  } catch (err: any) {
    error.value = err.message || 'Failed to load devices'
    showNotification('Failed to load devices', 'error')
  } finally {
    loading.value = false
  }
}

// Fetch locations
const fetchLocations = async () => {
  try {
    const response = await api.getAllLocationsLocations()
    locations.value = (response as any)?.data || []
  } catch (err) {
    console.error('Failed to load locations', err)
  }
}

// Refresh devices
const refreshDevices = () => {
  fetchDevices()
  showNotification('Refreshing device status...', 'success')
}

// Sync device
const syncDevice = async (deviceId: string) => {
  try {
    await api.syncDeviceDevices(deviceId, 'all')
    showNotification('Device sync initiated', 'success')
    await fetchDevices()
  } catch (err: any) {
    showNotification(err.message || 'Failed to sync device', 'error')
  }
}

// Test connection
const testConnection = async (deviceId: string) => {
  try {
    await api.testDeviceConnectionDevices(deviceId)
    showNotification('Connection test successful', 'success')
  } catch (err: any) {
    showNotification(err.message || 'Connection test failed', 'error')
  }
}

// Search with debounce
let searchTimeout: NodeJS.Timeout
const debouncedSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    currentPage.value = 1
    fetchDevices()
  }, 500)
}

// Pagination
const visiblePages = computed(() => {
  const pages = []
  const maxVisible = 5
  let start = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  const end = Math.min(totalPages.value, start + maxVisible - 1)
  
  if (end - start < maxVisible - 1) {
    start = Math.max(1, end - maxVisible + 1)
  }
  
  for (let i = start; i <= end; i++) {
    pages.push(i)
  }
  return pages
})

const goToPage = (page: number) => {
  currentPage.value = page
  fetchDevices()
}

// Utilities
const getDeviceStatusColor = (status?: string) => {
  switch (status?.toLowerCase()) {
    case 'online':
      return 'bg-green-500 animate-pulse'
    case 'offline':
      return 'bg-red-500'
    case 'syncing':
      return 'bg-blue-500 animate-pulse'
    default:
      return 'bg-gray-400'
  }
}

const getDeviceStatusBadgeClass = (status?: string) => {
  switch (status?.toLowerCase()) {
    case 'online':
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
    case 'offline':
      return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
    case 'syncing':
      return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
    default:
      return 'bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400'
  }
}

const formatRelativeTime = (date?: Date | string | null) => {
  if (!date) return 'Never'
  
  const d = new Date(date)
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  
  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  
  const diffHours = Math.floor(diffMins / 60)
  if (diffHours < 24) return `${diffHours}h ago`
  
  const diffDays = Math.floor(diffHours / 24)
  return `${diffDays}d ago`
}

// Initialize
onMounted(() => {
  fetchDevices()
  fetchLocations()
})
</script>
