<template>
  <div class="space-y-6">
    <!-- Breadcrumbs -->
    <AppNestedBreadcrumbs
      :items="[
        { name: 'Staff Directory', href: '/staff' },
        { name: staffName }
      ]"
    />

    <!-- Loading State -->
    <div v-if="loading" class="space-y-6">
      <div class="h-32 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="h-64 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
        <div class="lg:col-span-2 h-64 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-xl"></div>
      </div>
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

    <!-- Staff Detail -->
    <template v-else-if="staff">
      <!-- Header Card -->
      <AppCard class="relative">
        <div class="absolute top-6 right-6 flex gap-2">
          <AppButton
            variant="secondary"
            size="small"
            @click="navigateTo(`/staff/${staffId}/edit`)"
          >
            Edit Staff
          </AppButton>
          <AppButton
            variant="tertiary"
            size="small"
            @click="showDeactivateModal = true"
          >
            {{ staff.isActive ? 'Deactivate' : 'Activate' }}
          </AppButton>
        </div>

        <div class="flex flex-col md:flex-row gap-6">
          <div class="flex-shrink-0">
            <AppAvatar
              :name="`${staff.firstName} ${staff.lastName}`"
              size="xlarge"
            />
          </div>
          <div class="flex-1 space-y-4">
            <div>
              <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
                {{ staff.firstName }} {{ staff.lastName }}
              </h1>
              <p class="text-lg text-neutral-dark/70 dark:text-neutral-light/70">
                {{ staff.positionTitle || 'No Position Assigned' }}
              </p>
            </div>
            <div class="flex flex-wrap gap-2">
              <span
                :class="[
                  'px-3 py-1 text-sm rounded-full font-medium',
                  staff.isActive
                    ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                    : 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
                ]"
              >
                {{ staff.isActive ? 'Active' : 'Inactive' }}
              </span>
              <span
                :class="[
                  'px-3 py-1 text-sm rounded-full font-medium',
                  getEnrollmentStatusClass(staff.enrollmentStatus)
                ]"
              >
                {{ staff.enrollmentStatus || 'Not Enrolled' }}
              </span>
              <span class="px-3 py-1 text-sm rounded-full font-medium bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">
                {{ staff.employmentType || 'Not Specified' }}
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Main Content Grid -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Left Column - Contact & Basic Info -->
        <div class="space-y-6">
          <!-- Contact Information -->
          <AppCard>
            <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
              Contact Information
            </h2>
            <dl class="space-y-3">
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Email</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.email || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Phone</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.phone || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Mobile</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.mobile || '—' }}
                </dd>
              </div>
            </dl>
          </AppCard>

          <!-- Employment Details -->
          <AppCard>
            <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
              Employment Details
            </h2>
            <dl class="space-y-3">
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Employee ID</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1 font-mono">
                  {{ staff.employeeId || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Badge Number</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1 font-mono">
                  {{ staff.badgeNumber || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Department</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.department?.name || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Location</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.location?.name || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Shift</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ staff.shift?.name || '—' }}
                </dd>
              </div>
              <div>
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Hire Date</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ formatDate(staff.hireDate) }}
                </dd>
              </div>
              <div v-if="staff.terminationDate">
                <dt class="text-xs font-medium uppercase text-neutral-dark/60 dark:text-neutral-light/60">Termination Date</dt>
                <dd class="text-sm text-neutral-dark dark:text-neutral-light mt-1">
                  {{ formatDate(staff.terminationDate) }}
                </dd>
              </div>
            </dl>
          </AppCard>

          <!-- Quick Actions -->
          <AppCard>
            <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
              Quick Actions
            </h2>
            <div class="space-y-2">
              <AppButton
                variant="secondary"
                class="w-full justify-start"
                @click="navigateTo(`/attendance/records?staff=${staffId}`)"
              >
                View Attendance
              </AppButton>
              <AppButton
                variant="secondary"
                class="w-full justify-start"
                @click="navigateTo(`/enrollment/admin?staff=${staffId}`)"
              >
                Enroll Fingerprint
              </AppButton>
              <AppButton
                variant="secondary"
                class="w-full justify-start"
                @click="navigateTo(`/leave/requests?staff=${staffId}`)"
              >
                View Leave Requests
              </AppButton>
            </div>
          </AppCard>
        </div>

        <!-- Right Column - Detailed Info -->
        <div class="lg:col-span-2 space-y-6">
          <!-- Tab Navigation -->
          <div class="border-b border-neutral-light/70 dark:border-neutral-dark/70">
            <nav class="flex gap-8">
              <button
                v-for="tab in tabs"
                :key="tab.id"
                :class="[
                  'py-3 px-1 border-b-2 font-medium text-sm transition-colors',
                  activeTab === tab.id
                    ? 'border-primary text-primary'
                    : 'border-transparent text-neutral-dark/60 dark:text-neutral-light/60 hover:text-neutral-dark dark:hover:text-neutral-light'
                ]"
                @click="activeTab = tab.id"
              >
                {{ tab.name }}
              </button>
            </nav>
          </div>

          <!-- Tab Content -->
          <div v-if="activeTab === 'biometric'">
            <AppCard>
              <div class="flex items-center justify-between mb-4">
                <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light">
                  Biometric Enrollment Status
                </h2>
                <AppButton
                  variant="primary"
                  size="small"
                  @click="navigateTo(`/enrollment/admin?staff=${staffId}`)"
                >
                  Enroll Fingerprint
                </AppButton>
              </div>

              <div v-if="biometricTemplates.length > 0" class="space-y-4">
                <div
                  v-for="template in biometricTemplates"
                  :key="template.templateId"
                  class="flex items-center justify-between p-4 border border-neutral-light/70 dark:border-neutral-dark/70 rounded-lg"
                >
                  <div class="flex items-center gap-3">
                    <div class="h-10 w-10 rounded-full bg-primary/10 flex items-center justify-center">
                      <span class="text-primary font-semibold">{{ template.fingerId }}</span>
                    </div>
                    <div>
                      <p class="font-medium text-neutral-dark dark:text-neutral-light">
                        Finger {{ template.fingerId }}
                      </p>
                      <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                        Quality: {{ template.quality || 'N/A' }}%
                      </p>
                    </div>
                  </div>
                  <div class="flex items-center gap-2">
                    <span class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                      {{ formatDate(template.enrolledAt) }}
                    </span>
                    <button
                      class="text-red-600 hover:text-red-700 text-sm font-medium"
                      @click="deleteTemplate(template.templateId!)"
                    >
                      Delete
                    </button>
                  </div>
                </div>
              </div>
              <div v-else class="text-center py-8 text-neutral-dark/60 dark:text-neutral-light/60">
                No biometric templates enrolled yet
              </div>
            </AppCard>
          </div>

          <div v-else-if="activeTab === 'devices'">
            <AppCard>
              <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
                Device Enrollments
              </h2>

              <div v-if="deviceEnrollments.length > 0" class="space-y-3">
                <div
                  v-for="enrollment in deviceEnrollments"
                  :key="enrollment.enrollmentId"
                  class="flex items-center justify-between p-4 border border-neutral-light/70 dark:border-neutral-dark/70 rounded-lg"
                >
                  <div>
                    <p class="font-medium text-neutral-dark dark:text-neutral-light">
                      {{ enrollment.device?.name || 'Unknown Device' }}
                    </p>
                    <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                      Device ID: {{ enrollment.device?.serialNumber || enrollment.deviceId }}
                    </p>
                  </div>
                  <div class="text-right">
                    <span
                      :class="[
                        'px-2 py-1 text-xs rounded-full',
                        enrollment.isActive
                          ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                          : 'bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400'
                      ]"
                    >
                      {{ enrollment.isActive ? 'Active' : 'Inactive' }}
                    </span>
                    <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
                      Enrolled: {{ formatDate(enrollment.enrolledAt) }}
                    </p>
                  </div>
                </div>
              </div>
              <div v-else class="text-center py-8 text-neutral-dark/60 dark:text-neutral-light/60">
                Not enrolled on any devices yet
              </div>
            </AppCard>
          </div>

          <div v-else-if="activeTab === 'attendance'">
            <AppCard>
              <div class="flex items-center justify-between mb-4">
                <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light">
                  Recent Attendance
                </h2>
                <AppButton
                  variant="secondary"
                  size="small"
                  @click="navigateTo(`/attendance/records?staff=${staffId}`)"
                >
                  View All
                </AppButton>
              </div>

              <div v-if="loadingAttendance" class="space-y-3">
                <div v-for="i in 5" :key="i" class="h-16 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-lg"></div>
              </div>
              <div v-else-if="recentAttendance.length > 0" class="space-y-3">
                <div
                  v-for="record in recentAttendance"
                  :key="record.recordId"
                  class="flex items-center justify-between p-4 border border-neutral-light/70 dark:border-neutral-dark/70 rounded-lg"
                >
                  <div>
                    <p class="font-medium text-neutral-dark dark:text-neutral-light">
                      {{ formatDate(record.date) }}
                    </p>
                    <p class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">
                      {{ record.clockIn ? formatTime(record.clockIn) : '—' }} - {{ record.clockOut ? formatTime(record.clockOut) : '—' }}
                    </p>
                  </div>
                  <div class="text-right">
                    <p class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
                      {{ formatHours(record.totalHours) }}
                    </p>
                    <span
                      :class="[
                        'text-xs px-2 py-1 rounded-full',
                        getAttendanceStatusClass(record.status)
                      ]"
                    >
                      {{ record.status || 'Present' }}
                    </span>
                  </div>
                </div>
              </div>
              <div v-else class="text-center py-8 text-neutral-dark/60 dark:text-neutral-light/60">
                No attendance records found
              </div>
            </AppCard>

            <!-- Attendance Summary -->
            <AppCard>
              <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light mb-4">
                This Month Summary
              </h2>
              <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div class="text-center p-4 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg">
                  <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
                    {{ attendanceSummary.present }}
                  </p>
                  <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Present</p>
                </div>
                <div class="text-center p-4 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg">
                  <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
                    {{ attendanceSummary.late }}
                  </p>
                  <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Late</p>
                </div>
                <div class="text-center p-4 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg">
                  <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
                    {{ attendanceSummary.absent }}
                  </p>
                  <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Absent</p>
                </div>
                <div class="text-center p-4 bg-neutral-light/30 dark:bg-neutral-dark/30 rounded-lg">
                  <p class="text-2xl font-bold text-neutral-dark dark:text-neutral-light">
                    {{ attendanceSummary.totalHours }}
                  </p>
                  <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">Total Hours</p>
                </div>
              </div>
            </AppCard>
          </div>

          <div v-else-if="activeTab === 'leave'">
            <AppCard>
              <div class="flex items-center justify-between mb-4">
                <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light">
                  Leave Requests
                </h2>
                <AppButton
                  variant="secondary"
                  size="small"
                  @click="navigateTo(`/leave/requests?staff=${staffId}`)"
                >
                  View All
                </AppButton>
              </div>

              <div class="text-center py-8 text-neutral-dark/60 dark:text-neutral-light/60">
                Leave requests feature coming soon
              </div>
            </AppCard>
          </div>
        </div>
      </div>
    </template>

    <!-- Deactivate Modal -->
    <AppConfirmDialog
      v-if="showDeactivateModal"
      :title="staff?.isActive ? 'Deactivate Staff Member' : 'Activate Staff Member'"
      :message="`Are you sure you want to ${staff?.isActive ? 'deactivate' : 'activate'} ${staff?.firstName} ${staff?.lastName}?`"
      @confirm="toggleStaffStatus"
      @cancel="showDeactivateModal = false"
    />
  </div>
</template>

<script setup lang="ts">
import type { Staff, BiometricTemplate, DeviceEnrollment, AttendanceRecord } from '~/lib/punch-clock-api'

const route = useRoute()
const api = usePunchClockApi()
const { showNotification } = useNotificationStore()

// Route params
const staffId = computed(() => route.params.id as string)

// State
const staff = ref<Staff | null>(null)
const biometricTemplates = ref<BiometricTemplate[]>([])
const deviceEnrollments = ref<DeviceEnrollment[]>([])
const recentAttendance = ref<AttendanceRecord[]>([])
const loading = ref(false)
const loadingAttendance = ref(false)
const error = ref<string | null>(null)
const showDeactivateModal = ref(false)

// Tabs
const activeTab = ref('biometric')
const tabs = [
  { id: 'biometric', name: 'Biometric Status' },
  { id: 'devices', name: 'Device Enrollments' },
  { id: 'attendance', name: 'Attendance' },
  { id: 'leave', name: 'Leave' }
]

// Attendance summary
const attendanceSummary = computed(() => {
  // This would come from API in real implementation
  return {
    present: 18,
    late: 3,
    absent: 2,
    totalHours: '152.5'
  }
})

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
    
    // Extract related data
    biometricTemplates.value = staff.value?.biometricTemplates || []
    deviceEnrollments.value = staff.value?.deviceEnrollments || []
  } catch (err: any) {
    error.value = err.message || 'Failed to load staff details'
    showNotification('Failed to load staff details', 'error')
  } finally {
    loading.value = false
  }
}

// Fetch recent attendance
const fetchRecentAttendance = async () => {
  loadingAttendance.value = true
  try {
    const endDate = new Date()
    const startDate = new Date()
    startDate.setDate(startDate.getDate() - 30)
    
    const response = await api.getAttendanceRecordsAttendance(
      startDate,
      endDate,
      staffId.value,
      1,
      10,
      'date',
      'desc'
    )
    recentAttendance.value = (response as any)?.data || []
  } catch (err) {
    console.error('Failed to load attendance', err)
  } finally {
    loadingAttendance.value = false
  }
}

// Toggle staff status
const toggleStaffStatus = async () => {
  try {
    await api.updateStaffStaff(staffId.value, {
      ...staff.value,
      isActive: !staff.value?.isActive
    })
    
    showNotification(
      `Staff ${staff.value?.isActive ? 'deactivated' : 'activated'} successfully`,
      'success'
    )
    showDeactivateModal.value = false
    await fetchStaffDetail()
  } catch (err: any) {
    showNotification(err.message || 'Failed to update staff status', 'error')
  }
}

// Delete biometric template
const deleteTemplate = async (templateId: string) => {
  if (!confirm('Are you sure you want to delete this biometric template?')) return
  
  try {
    await api.deleteTemplateBiometrics(templateId)
    showNotification('Biometric template deleted successfully', 'success')
    await fetchStaffDetail()
  } catch (err: any) {
    showNotification(err.message || 'Failed to delete template', 'error')
  }
}

// Utilities
const formatDate = (date?: Date | string | null) => {
  if (!date) return '—'
  const d = new Date(date)
  return d.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })
}

const formatTime = (time?: string | null) => {
  if (!time) return '—'
  return time
}

const formatHours = (hours?: number | string | null) => {
  if (!hours) return '0h'
  const h = typeof hours === 'string' ? parseFloat(hours) : hours
  return `${h.toFixed(1)}h`
}

const getEnrollmentStatusClass = (status?: string) => {
  switch (status?.toLowerCase()) {
    case 'enrolled':
      return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
    case 'pending':
      return 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'
    default:
      return 'bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400'
  }
}

const getAttendanceStatusClass = (status?: string) => {
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

// Initialize
onMounted(() => {
  fetchStaffDetail()
  fetchRecentAttendance()
})
</script>
