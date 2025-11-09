<template>
  <div class="space-y-6">
    <!-- Page Header -->
    <div>
      <h1 class="text-3xl font-bold text-neutral-dark dark:text-neutral-light">
        Reports & Analytics
      </h1>
      <p class="mt-1 text-sm text-neutral-dark/70 dark:text-neutral-light/70">
        Generate and download attendance, payroll, and performance reports
      </p>
    </div>

    <!-- Quick Report Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <!-- Daily Report -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="navigateTo('/reports/daily')">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-blue-100 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
            <CalendarDaysIcon class="h-6 w-6 text-blue-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Daily Attendance Report
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              View and export daily attendance records with filters
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400">
                Real-time
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Monthly Report -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="navigateTo('/reports/monthly')">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-green-100 dark:bg-green-900/30 flex items-center justify-center flex-shrink-0">
            <ChartBarIcon class="h-6 w-6 text-green-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Monthly Attendance Report
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              Comprehensive monthly summaries and attendance rates
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400">
                Summary
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Payroll Export -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="navigateTo('/reports/payroll')">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center flex-shrink-0">
            <DocumentTextIcon class="h-6 w-6 text-purple-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Payroll Export
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              Export hours with overtime calculation for payroll
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400">
                Export
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Department Performance -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="navigateTo('/reports/department')">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-orange-100 dark:bg-orange-900/30 flex items-center justify-center flex-shrink-0">
            <BuildingOfficeIcon class="h-6 w-6 text-orange-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Department Performance
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              Compare attendance rates across departments
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400">
                Analytics
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Custom Report Builder -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="showCustomBuilder = true">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-teal-100 dark:bg-teal-900/30 flex items-center justify-center flex-shrink-0">
            <DocumentChartBarIcon class="h-6 w-6 text-teal-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Custom Report Builder
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              Create custom reports with advanced filters
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-teal-100 text-teal-700 dark:bg-teal-900/30 dark:text-teal-400">
                Advanced
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <!-- Export History -->
      <AppCard class="hover:shadow-xl transition-shadow cursor-pointer" @click="navigateTo('/reports/history')">
        <div class="flex items-start gap-4">
          <div class="h-12 w-12 rounded-lg bg-gray-100 dark:bg-gray-900/30 flex items-center justify-center flex-shrink-0">
            <ClockIcon class="h-6 w-6 text-gray-600" />
          </div>
          <div class="flex-1">
            <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light">
              Export History
            </h3>
            <p class="text-sm text-neutral-dark/60 dark:text-neutral-light/60 mt-1">
              View and download previous report exports
            </p>
            <div class="mt-3 flex items-center gap-2">
              <span class="text-xs px-2 py-1 rounded-full bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400">
                History
              </span>
            </div>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Recent Reports -->
    <AppCard>
      <div class="flex items-center justify-between mb-4">
        <h2 class="text-xl font-semibold text-neutral-dark dark:text-neutral-light">
          Recent Reports
        </h2>
        <AppButton
          variant="secondary"
          size="small"
          @click="navigateTo('/reports/history')"
        >
          View All
        </AppButton>
      </div>

      <div v-if="loadingRecent" class="space-y-3">
        <div v-for="i in 5" :key="i" class="h-16 bg-neutral-light/50 dark:bg-neutral-dark/50 animate-pulse rounded-lg"></div>
      </div>

      <div v-else-if="recentReports.length > 0" class="space-y-3">
        <div
          v-for="report in recentReports"
          :key="report.id"
          class="flex items-center justify-between p-4 border border-neutral-light/70 dark:border-neutral-dark/70 rounded-lg hover:bg-neutral-light/20 dark:hover:bg-neutral-dark/20 transition-colors"
        >
          <div class="flex items-center gap-4">
            <div class="h-10 w-10 rounded-lg bg-primary/10 flex items-center justify-center">
              <DocumentTextIcon class="h-5 w-5 text-primary" />
            </div>
            <div>
              <p class="font-medium text-neutral-dark dark:text-neutral-light">
                {{ report.name }}
              </p>
              <p class="text-xs text-neutral-dark/60 dark:text-neutral-light/60">
                Generated {{ formatDate(report.createdAt) }} • {{ report.size }}
              </p>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <span
              :class="[
                'px-2 py-1 text-xs rounded-full',
                report.status === 'completed'
                  ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                  : 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400'
              ]"
            >
              {{ report.status }}
            </span>
            <AppButton
              v-if="report.status === 'completed'"
              variant="tertiary"
              size="small"
              @click="downloadReport(report.id)"
            >
              Download
            </AppButton>
          </div>
        </div>
      </div>

      <div v-else class="text-center py-8 text-neutral-dark/60 dark:text-neutral-light/60">
        No recent reports found
      </div>
    </AppCard>

    <!-- Quick Stats -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <AppCard>
        <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light mb-4">
          This Month
        </h3>
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">Avg Attendance</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">94.2%</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">Total Hours</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">8,432</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">Overtime Hours</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">342</span>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light mb-4">
          Top Departments
        </h3>
        <div class="space-y-3">
          <div v-for="dept in topDepartments" :key="dept.name" class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">{{ dept.name }}</span>
            <div class="flex items-center gap-2">
              <div class="w-24 h-2 bg-neutral-light/50 dark:bg-neutral-dark/50 rounded-full overflow-hidden">
                <div
                  class="h-full bg-primary rounded-full"
                  :style="{ width: `${dept.rate}%` }"
                ></div>
              </div>
              <span class="text-sm font-medium text-neutral-dark dark:text-neutral-light">
                {{ dept.rate }}%
              </span>
            </div>
          </div>
        </div>
      </AppCard>

      <AppCard>
        <h3 class="text-lg font-semibold text-neutral-dark dark:text-neutral-light mb-4">
          Report Exports
        </h3>
        <div class="space-y-3">
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">This Week</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">23</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">This Month</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">87</span>
          </div>
          <div class="flex items-center justify-between">
            <span class="text-sm text-neutral-dark/60 dark:text-neutral-light/60">Total Size</span>
            <span class="text-lg font-bold text-neutral-dark dark:text-neutral-light">142 MB</span>
          </div>
        </div>
      </AppCard>
    </div>

    <!-- Custom Report Builder Modal -->
    <AppModal v-if="showCustomBuilder" @close="showCustomBuilder = false">
      <template #header>
        Custom Report Builder
      </template>
      <div class="space-y-4">
        <p class="text-sm text-neutral-dark/70 dark:text-neutral-light/70">
          Build custom reports with advanced filtering and export options
        </p>
        
        <div class="space-y-3">
          <div>
            <label class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
              Report Type
            </label>
            <select class="w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm">
              <option>Attendance Summary</option>
              <option>Hours Breakdown</option>
              <option>Leave Analysis</option>
              <option>Overtime Report</option>
            </select>
          </div>
          
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
                Start Date
              </label>
              <input
                type="date"
                class="w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
                End Date
              </label>
              <input
                type="date"
                class="w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm"
              />
            </div>
          </div>
          
          <div>
            <label class="block text-sm font-medium text-neutral-dark dark:text-neutral-light mb-1">
              Export Format
            </label>
            <select class="w-full rounded-lg border border-neutral-light/70 dark:border-neutral-dark/70 bg-white dark:bg-neutral-dark px-3 py-2 text-sm">
              <option>Excel (.xlsx)</option>
              <option>CSV (.csv)</option>
              <option>PDF (.pdf)</option>
            </select>
          </div>
        </div>
        
        <div class="flex gap-3 pt-4">
          <AppButton variant="primary" class="flex-1" @click="generateCustomReport">
            Generate Report
          </AppButton>
          <AppButton variant="tertiary" @click="showCustomBuilder = false">
            Cancel
          </AppButton>
        </div>
      </div>
    </AppModal>
  </div>
</template>

<script setup lang="ts">
import {
  CalendarDaysIcon,
  ChartBarIcon,
  DocumentTextIcon,
  BuildingOfficeIcon,
  DocumentChartBarIcon,
  ClockIcon
} from '@heroicons/vue/24/outline'

const { showNotification } = useNotificationStore()

// State
const loadingRecent = ref(false)
const showCustomBuilder = ref(false)

// Mock data for recent reports
const recentReports = ref([
  {
    id: '1',
    name: 'Daily Attendance - 2025-11-08',
    createdAt: new Date('2025-11-08'),
    size: '2.3 MB',
    status: 'completed'
  },
  {
    id: '2',
    name: 'Monthly Report - October 2025',
    createdAt: new Date('2025-11-01'),
    size: '5.1 MB',
    status: 'completed'
  },
  {
    id: '3',
    name: 'Payroll Export - Week 44',
    createdAt: new Date('2025-11-05'),
    size: '1.8 MB',
    status: 'completed'
  }
])

// Mock data for top departments
const topDepartments = ref([
  { name: 'HR', rate: 98 },
  { name: 'Sales', rate: 95 },
  { name: 'Engineering', rate: 94 },
  { name: 'Operations', rate: 92 }
])

// Download report
const downloadReport = (reportId: string) => {
  showNotification('Downloading report...', 'success')
  // Implement download logic
}

// Generate custom report
const generateCustomReport = () => {
  showNotification('Generating custom report...', 'success')
  showCustomBuilder.value = false
  // Implement custom report generation
}

// Utilities
const formatDate = (date: Date) => {
  return date.toLocaleDateString('en-US', { 
    year: 'numeric', 
    month: 'short', 
    day: 'numeric' 
  })
}
</script>
