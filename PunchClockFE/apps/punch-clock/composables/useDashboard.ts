/**
 * Dashboard Data Composable
 * Fetches and manages dashboard statistics and real-time data
 */
import { usePunchClockApi } from './usePunchClockApi'

export interface DashboardStats {
  totalStaff: number
  clockedIn: number
  lateArrivals: number
  pendingCorrections: number
  activeDevices: number
  totalDevices: number
  lastSync: string
  pendingLeaveRequests: number
}

export const useDashboard = () => {
  const api = usePunchClockApi()
  const stats = ref<DashboardStats>({
    totalStaff: 0,
    clockedIn: 0,
    lateArrivals: 0,
    pendingCorrections: 0,
    activeDevices: 0,
    totalDevices: 0,
    lastSync: 'Loading...',
    pendingLeaveRequests: 0,
  })

  const loading = ref(false)
  const error = ref<string | null>(null)

  /**
   * Format date for API (without timezone suffix to match backend format)
   */
  const formatDateForApi = (date: Date): string => {
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    const seconds = String(date.getSeconds()).padStart(2, '0')
    return `${year}-${month}-${day}T${hours}:${minutes}:${seconds}`
  }

  /**
   * Fetch dashboard statistics
   */
  const fetchDashboardStats = async () => {
    loading.value = true
    error.value = null

    try {
      const today = new Date()
      today.setHours(0, 0, 0, 0)
      const now = new Date()

      // Fetch data in parallel
      const [
        staffResponse,
        attendanceResponse,
        correctionsResponse,
        devicesResponse,
        leaveRequestsResponse,
      ] = await Promise.all([
        // Total active staff
        $fetch('/api/staff', {
          params: { isActive: true, limit: 1 },
        }).catch(() => ({ data: [], total: 0 })),

        // Today's attendance records
        $fetch('/api/attendance/records', {
          params: {
            startDate: formatDateForApi(today),
            endDate: formatDateForApi(now),
            limit: 1000,
          },
        }).catch(() => ({ data: [], total: 0 })),

        // Pending corrections
        $fetch('/api/attendance/corrections', {
          params: { status: 'pending', limit: 1 },
        }).catch(() => ({ data: [], total: 0 })),

        // Devices
        $fetch('/api/devices', {
          params: { limit: 1000 },
        }).catch(() => ({ data: [], total: 0, active: 0 })),

        // Pending leave requests
        $fetch('/api/leave/requests', {
          params: { status: 'pending', limit: 1 },
        }).catch(() => ({ data: [], total: 0 })),
      ])

      // Calculate statistics
      stats.value.totalStaff = (staffResponse as any)?.total || 0
      
      // Count clocked in today (attendance records with clockIn)
      const attendanceData = (attendanceResponse as any)?.data || []
      stats.value.clockedIn = attendanceData.filter((record: any) => record.clockIn).length
      
      // Count late arrivals
      stats.value.lateArrivals = attendanceData.filter(
        (record: any) => record.lateMinutes && record.lateMinutes > 0
      ).length

      stats.value.pendingCorrections = (correctionsResponse as any)?.total || 0

      // Device statistics
      const devicesData = (devicesResponse as any)?.data || []
      stats.value.totalDevices = devicesData.length
      stats.value.activeDevices = devicesData.filter((d: any) => d.isActive).length

      stats.value.pendingLeaveRequests = (leaveRequestsResponse as any)?.total || 0

      // Calculate last sync time (most recent device sync)
      if (devicesData.length > 0) {
        const lastSyncDate = devicesData.reduce((latest: Date | null, device: any) => {
          const deviceSync = device.lastSyncTime ? new Date(device.lastSyncTime) : null
          if (!latest || (deviceSync && deviceSync > latest)) {
            return deviceSync
          }
          return latest
        }, null)

        if (lastSyncDate) {
          const minutes = Math.floor((Date.now() - lastSyncDate.getTime()) / 60000)
          if (minutes < 1) {
            stats.value.lastSync = 'Just now'
          } else if (minutes === 1) {
            stats.value.lastSync = '1 minute ago'
          } else if (minutes < 60) {
            stats.value.lastSync = `${minutes} minutes ago`
          } else {
            const hours = Math.floor(minutes / 60)
            stats.value.lastSync = hours === 1 ? '1 hour ago' : `${hours} hours ago`
          }
        } else {
          stats.value.lastSync = 'Never'
        }
      }
    } catch (err) {
      console.error('Failed to fetch dashboard stats:', err)
      error.value = err instanceof Error ? err.message : 'Failed to load dashboard data'
    } finally {
      loading.value = false
    }
  }

  /**
   * Check system health
   */
  const checkSystemHealth = async () => {
    try {
      // Check if we can reach the API by testing a simple endpoint
      await $fetch('/api/devices', { params: { limit: 1 } })
      return { status: 'healthy' }
    } catch (err) {
      console.error('Health check failed:', err)
      return { status: 'unhealthy', error: err }
    }
  }

  return {
    stats: readonly(stats),
    loading: readonly(loading),
    error: readonly(error),
    fetchDashboardStats,
    checkSystemHealth,
  }
}
