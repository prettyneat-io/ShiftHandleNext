<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
      <div>
        <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
          Live Attendance
        </h1>
        <p class="mt-1 text-sm text-neutral-dark/70 dark:text-neutral-light/70">
          Real-time attendance monitoring for {{ formatDate(currentDate) }}
        </p>
      </div>
      <div class="flex flex-wrap gap-3 items-center">
        <div class="flex items-center gap-2 text-sm">
          <div class="h-2 w-2 rounded-full bg-green-500 animate-pulse"></div>
          <span class="text-neutral-dark/70 dark:text-neutral-light/70">
            Auto-refresh: {{ autoRefreshEnabled ? 'On' : 'Off' }}
          </span>
        </div>
        <button
          :class="[
            'px-3 py-1 rounded-md text-sm transition-colors',
            autoRefreshEnabled
              ? 'bg-primary text-white'
              : 'bg-neutral-light/30 dark:bg-neutral-dark/30'
          ]"
          @click="toggleAutoRefresh"
        >
          {{ autoRefreshEnabled ? 'Stop' : 'Start' }}
        </button>
        <AppButton
          variant="secondary"
          size="small"
          @click="refreshData"
        >
          Refresh Now
        </AppButton>
      </div>
    </div>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
      <AppCard>
        <div class="flex items-center gap-4">
          <div class="h-12 w-12 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center">
            <UsersIcon class="h-6 w-6 text-blue-600" />
          </div>
          <div>
            <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
              {{ stats.totalScheduled }}
            </p>
            <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Total Scheduled</p>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <div class="flex items-center gap-4">
          <div class="h-12 w-12 rounded-lg bg-green-100 dark:bg-green-900/30 flex items-center justify-center">
            <CheckCircleIcon class="h-6 w-6 text-green-600" />
          </div>
          <div>
            <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
              {{ stats.clockedIn }}
            </p>
            <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Clocked In</p>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <div class="flex items-center gap-4">
          <div class="h-12 w-12 rounded-lg bg-orange-100 dark:bg-orange-900/30 flex items-center justify-center">
            <ClockIcon class="h-6 w-6 text-orange-600" />
          </div>
          <div>
            <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
              {{ stats.lateArrivals }}
            </p>
            <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Late Arrivals</p>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <div class="flex items-center gap-4">
          <div class="h-12 w-12 rounded-lg bg-red-100 dark:bg-red-900/30 flex items-center justify-center">
            <ExclamationTriangleIcon class="h-6 w-6 text-red-600" />
          </div>
          <div>
            <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
              {{ stats.absent }}
            </p>
            <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Absent</p>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Filters -->
    <AppCard>
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
        <AppInput
          v-model="filters.search"
          placeholder="Search staff..."
          @update:model-value="debouncedSearch"
        />
        <select
          v-model="filters.department"
          class="rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
          @change="fetchAttendance"
        >
          <option value="">All Departments</option>
          <option v-for="dept in departments" :key="dept.departmentId" :value="dept.departmentId">
            {{ dept.name }}
          </option>
        </select>
        <select
          v-model="filters.status"
          class="rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
          @change="fetchAttendance"
        >
          <option value="">All Status</option>
          <option value="clocked-in">Clocked In</option>
          <option value="clocked-out">Clocked Out</option>
          <option value="late">Late</option>
          <option value="absent">Absent</option>
        </select>
        <select
          v-model="filters.location"
          class="rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
          @change="fetchAttendance"
        >
          <option value="">All Locations</option>
          <option v-for="loc in locations" :key="loc.locationId" :value="loc.locationId">
            {{ loc.name }}
          </option>
        </select>
      </div>
    </AppCard>

    <!-- Loading State -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 10" :key="i" class="h-20 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-lg"></div>
    </div>

    <!-- Error State -->
    <AppCard v-else-if="error">
      <div class="text-center py-8">
        <p class="text-red-600 dark:text-red-400">{{ error }}</p>
        <AppButton variant="primary" size="small" class="mt-4" @click="fetchAttendance">
          Retry
        </AppButton>
      </div>
    </AppCard>

    <!-- Attendance List -->
    <AppCard v-else>
      <div class="overflow-x-auto">
        <table class="w-full">
          <thead class="border-b border-neutral-light/70 dark:border-neutral-dark/70">
            <tr>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Staff
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Department
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Scheduled
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Clock In
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Clock Out
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Hours
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Status
              </th>
              <th class="px-4 py-3 text-left text-sm font-semibold text-neutral-dark dark:text-neutral-light">
                Actions
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="record in attendanceRecords"
              :key="record.recordId || record.staffId"
              class="border-b border-neutral-light/50 dark:border-neutral-dark/50 hover:bg-neutral-light/20 dark:hover:bg-neutral-dark/20"
            >
              <td class="px-4 py-3">
                <div class="flex items-center gap-3">
                  <AppAvatar
                    :name="`${record.staff?.firstName} ${record.staff?.lastName}`"
                    size="small"
                  />
                  <div>
                    <p class="font-medium text-neutral-dark dark:text-neutral-light">
                      {{ record.staff?.firstName }} {{ record.staff?.lastName }}
                    </p>
                    <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                      {{ record.staff?.badgeNumber || '—' }}
                    </p>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3 text-sm text-neutral-dark dark:text-neutral-light">
                {{ record.staff?.department?.name || '—' }}
              </td>
              <td class="px-4 py-3 text-sm text-neutral-dark dark:text-neutral-light">
                {{ record.scheduledIn || '—' }}
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <span class="text-sm text-neutral-dark dark:text-neutral-light font-medium">
                    {{ record.clockIn || '—' }}
                  </span>
                  <span
                    v-if="record.clockIn && isLate(record.scheduledIn, record.clockIn)"
                    class="px-2 py-0.5 text-xs rounded-full bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400"
                  >
                    Late
                  </span>
                </div>
              </td>
              <td class="px-4 py-3 text-sm text-neutral-dark dark:text-neutral-light">
                {{ record.clockOut || '—' }}
              </td>
              <td class="px-4 py-3 text-sm text-neutral-dark dark:text-neutral-light font-medium">
                {{ formatHours(record.totalHours) }}
              </td>
              <td class="px-4 py-3">
                <span
                  :class="[
                    'px-3 py-1 text-xs rounded-full font-medium',
                    getStatusBadgeClass(record.status)
                  ]"
                >
                  {{ getStatusLabel(record) }}
                </span>
              </td>
              <td class="px-4 py-3">
                <button
                  class="text-primary hover:text-primary-dark text-sm font-medium"
                  @click="navigateTo(`/staff/${record.staffId}`)"
                >
                  View
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Empty State -->
      <div v-if="attendanceRecords.length === 0" class="text-center py-12">
        <UsersIcon class="h-12 w-12 text-neutral-dark/20 dark:text-neutral-light/20 mx-auto mb-3" />
        <p class="text-neutral-dark/60 dark:text-neutral-light/60">
          No attendance records found for today
        </p>
      </div>
    </AppCard>

    <!-- Department Breakdown -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <AppCard>
        <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
          Department Breakdown
        </h2>
        <div class="space-y-3">
          <div
            v-for="dept in departmentStats"
            :key="dept.departmentId"
            class="flex items-center justify-between p-3 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg"
          >
            <div>
              <p class="font-medium text-neutral-dark dark:text-neutral-light">
                {{ dept.name }}
              </p>
              <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                {{ dept.present }} / {{ dept.total }} present
              </p>
            </div>
            <div class="text-right">
              <p class="text-lg font-bold text-neutral-dark dark:text-neutral-light">
                {{ dept.attendanceRate }}%
              </p>
              <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                {{ dept.late }} late
              </p>
            </div>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
          Recent Activity
        </h2>
        <div class="space-y-3 max-h-96 overflow-y-auto">
          <div
            v-for="activity in recentActivity"
            :key="activity.id"
            class="flex items-start gap-3 p-3 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg"
          >
            <div class="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
              <ClockIcon class="h-4 w-4 text-primary" />
            </div>
            <div class="flex-1">
              <p class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
                {{ activity.staffName }}
              </p>
              <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                {{ activity.action }} at {{ activity.time }}
              </p>
            </div>
            <span class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
              {{ formatRelativeTime(activity.timestamp) }}
            </span>
          </div>
        </div>
      </AppCard>
    </div>
  </div>
</template>

<script setup lang="ts">
import {
  UsersIcon,
  CheckCircleIcon,
  ClockIcon,
  ExclamationTriangleIcon
} from '@heroicons/vue/24/outline'
import type { AttendanceRecord } from '~/lib/punch-clock-api'

const api = usePunchClockApi()
const { showNotification } = useNotificationStore()

// State
const attendanceRecords = ref<AttendanceRecord[]>([])
const departments = ref<any[]>([])
const locations = ref<any[]>([])
const loading = ref(false)
const error = ref<string | null>(null)
const currentDate = ref(new Date())
const autoRefreshEnabled = ref(true)
let refreshInterval: NodeJS.Timeout | null = null

// Filters
const filters = ref({
  search: '',
  department: '',
  status: '',
  location: ''
})

// Stats
const stats = computed(() => {
  const total = attendanceRecords.value.length
  const clockedIn = attendanceRecords.value.filter(r => r.clockIn && !r.clockOut).length
  const late = attendanceRecords.value.filter(r => 
    r.clockIn && r.scheduledIn && isLate(r.scheduledIn, r.clockIn)
  ).length
  const absent = attendanceRecords.value.filter(r => !r.clockIn).length

  return {
    totalScheduled: total,
    clockedIn,
    lateArrivals: late,
    absent
  }
})

// Department stats (mock data - would come from API)
const departmentStats = computed(() => {
  return [
    { departmentId: '1', name: 'Engineering', total: 25, present: 23, late: 2, attendanceRate: 92 },
    { departmentId: '2', name: 'Sales', total: 18, present: 17, late: 1, attendanceRate: 94 },
    { departmentId: '3', name: 'Operations', total: 32, present: 28, late: 4, attendanceRate: 88 },
    { departmentId: '4', name: 'HR', total: 8, present: 8, late: 0, attendanceRate: 100 }
  ]
})

// Recent activity (mock data - would come from SignalR in real implementation)
const recentActivity = ref([
  { id: 1, staffName: 'John Doe', action: 'Clocked In', time: '09:02', timestamp: new Date() },
  { id: 2, staffName: 'Jane Smith', action: 'Clocked Out', time: '17:30', timestamp: new Date(Date.now() - 300000) },
  { id: 3, staffName: 'Mike Johnson', action: 'Clocked In', time: '08:55', timestamp: new Date(Date.now() - 600000) }
])

// Fetch attendance data
const fetchAttendance = async () => {
  loading.value = true
  error.value = null
  try {
    const startDate = new Date()
    startDate.setHours(0, 0, 0, 0)
    const endDate = new Date()
    endDate.setHours(23, 59, 59, 999)

    const response = await api.getAttendanceRecordsAttendance(
      startDate,
      endDate,
      undefined,
      1,
      100,
      'clockIn',
      'asc',
      'staff,staff.department'
    )

    attendanceRecords.value = (response as any)?.data || []
  } catch (err: any) {
    error.value = err.message || 'Failed to load attendance'
    showNotification('Failed to load attendance', 'error')
  } finally {
    loading.value = false
  }
}

// Fetch departments and locations
const fetchDepartments = async () => {
  try {
    const response = await api.getAllDepartmentsDepartments()
    departments.value = (response as any)?.data || []
  } catch (err) {
    console.error('Failed to load departments', err)
  }
}

const fetchLocations = async () => {
  try {
    const response = await api.getAllLocationsLocations()
    locations.value = (response as any)?.data || []
  } catch (err) {
    console.error('Failed to load locations', err)
  }
}

// Refresh data
const refreshData = async () => {
  await fetchAttendance()
  showNotification('Attendance data refreshed', 'success')
}

// Auto-refresh toggle
const toggleAutoRefresh = () => {
  autoRefreshEnabled.value = !autoRefreshEnabled.value
  
  if (autoRefreshEnabled.value) {
    startAutoRefresh()
    showNotification('Auto-refresh enabled', 'success')
  } else {
    stopAutoRefresh()
    showNotification('Auto-refresh disabled', 'success')
  }
}

const startAutoRefresh = () => {
  if (refreshInterval) clearInterval(refreshInterval)
  
  refreshInterval = setInterval(() => {
    fetchAttendance()
  }, 30000) // Refresh every 30 seconds
}

const stopAutoRefresh = () => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
    refreshInterval = null
  }
}

// Search with debounce
let searchTimeout: NodeJS.Timeout
const debouncedSearch = () => {
  clearTimeout(searchTimeout)
  searchTimeout = setTimeout(() => {
    fetchAttendance()
  }, 500)
}

// Utilities
const isLate = (scheduled?: string, actual?: string) => {
  if (!scheduled || !actual) return false
  
  const [schedHour, schedMin] = scheduled.split(':').map(Number)
  const [actHour, actMin] = actual.split(':').map(Number)
  
  const schedMinutes = schedHour * 60 + schedMin
  const actMinutes = actHour * 60 + actMin
  
  return actMinutes > schedMinutes + 5 // 5 minute grace period
}

const formatHours = (hours?: number | string | null) => {
  if (!hours) return '0h'
  const h = typeof hours === 'string' ? parseFloat(hours) : hours
  return `${h.toFixed(1)}h`
}

const formatDate = (date: Date) => {
  return date.toLocaleDateString('en-US', { 
    weekday: 'long', 
    year: 'numeric', 
    month: 'long', 
    day: 'numeric' 
  })
}

const formatRelativeTime = (date: Date) => {
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  
  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  
  const diffHours = Math.floor(diffMins / 60)
  return `${diffHours}h ago`
}

const getStatusBadgeClass = (status?: string) => {
  switch (status?.toLowerCase()) {
    case 'present':
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
    case 'late':
      return 'bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400'
    case 'absent':
      return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
    default:
      return 'bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400'
  }
}

const getStatusLabel = (record: AttendanceRecord) => {
  if (!record.clockIn) return 'Absent'
  if (!record.clockOut) return 'Clocked In'
  if (record.scheduledIn && isLate(record.scheduledIn, record.clockIn)) return 'Late'
  return 'Present'
}

// Lifecycle
onMounted(() => {
  fetchAttendance()
  fetchDepartments()
  fetchLocations()
  
  if (autoRefreshEnabled.value) {
    startAutoRefresh()
  }
})

onUnmounted(() => {
  stopAutoRefresh()
})
</script>
