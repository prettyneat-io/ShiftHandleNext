/**
 * Punch Clock Navigation Configuration
 * Based on FRONTEND_SPEC.md sitemap
 */
import {
  HomeIcon,
  UsersIcon,
  ComputerDesktopIcon,
  ClockIcon,
  DocumentChartBarIcon,
  BuildingOfficeIcon,
  CalendarDaysIcon,
  Cog6ToothIcon,
  UserCircleIcon,
  FingerPrintIcon,
} from '@heroicons/vue/24/outline'
import type { NavigationGroup } from '~/types/navigation'

export const navigation: NavigationGroup[] = [
  {
    title: 'Dashboard',
    icon: HomeIcon,
    items: [
      { name: 'Dashboard', href: '/', icon: HomeIcon },
      { name: 'Live Attendance', href: '/attendance/live', icon: ClockIcon },
    ],
  },
  {
    title: 'Staff Management',
    icon: UsersIcon,
    items: [
      { name: 'Staff Directory', href: '/staff', icon: UsersIcon },
      { name: 'Bulk Import', href: '/staff/import', icon: DocumentChartBarIcon },
      { name: 'Enrollment Status', href: '/staff/enrollment-status', icon: FingerPrintIcon },
    ],
  },
  {
    title: 'Biometric Enrollment',
    icon: FingerPrintIcon,
    items: [
      { name: 'Self-Service Enrollment', href: '/enrollment/self-service', icon: FingerPrintIcon },
      { name: 'Admin Enrollment', href: '/enrollment/admin', icon: UserCircleIcon },
      { name: 'Bulk Enrollment', href: '/enrollment/bulk', icon: UsersIcon },
      { name: 'Template Management', href: '/enrollment/templates', icon: DocumentChartBarIcon },
    ],
  },
  {
    title: 'Device Management',
    icon: ComputerDesktopIcon,
    items: [
      { name: 'Device Dashboard', href: '/devices', icon: ComputerDesktopIcon },
      { name: 'Sync Management', href: '/devices/sync', icon: ClockIcon },
    ],
  },
  {
    title: 'Attendance & Time',
    icon: ClockIcon,
    items: [
      { name: 'Calendar View', href: '/attendance/calendar', icon: CalendarDaysIcon },
      { name: 'Punch Logs', href: '/attendance/logs', icon: DocumentChartBarIcon },
      { name: 'Attendance Records', href: '/attendance/records', icon: ClockIcon },
      { name: 'Missing Punches', href: '/attendance/missing', icon: ClockIcon },
      { name: 'Anomalies', href: '/attendance/anomalies', icon: DocumentChartBarIcon },
    ],
  },
  {
    title: 'Leave Management',
    icon: CalendarDaysIcon,
    items: [
      { name: 'Leave Requests', href: '/leave/requests', icon: CalendarDaysIcon },
      { name: 'Submit Request', href: '/leave/submit', icon: CalendarDaysIcon },
      { name: 'Approval Workflow', href: '/leave/approval', icon: DocumentChartBarIcon },
      { name: 'Leave Balances', href: '/leave/balances', icon: DocumentChartBarIcon },
      { name: 'Leave Types', href: '/leave/types', icon: Cog6ToothIcon },
    ],
  },
  {
    title: 'Reports & Analytics',
    icon: DocumentChartBarIcon,
    items: [
      { name: 'Reports Dashboard', href: '/reports', icon: DocumentChartBarIcon },
      { name: 'Daily Report', href: '/reports/daily', icon: DocumentChartBarIcon },
      { name: 'Monthly Report', href: '/reports/monthly', icon: DocumentChartBarIcon },
      { name: 'Payroll Export', href: '/reports/payroll', icon: DocumentChartBarIcon },
      { name: 'Department Performance', href: '/reports/department', icon: BuildingOfficeIcon },
      { name: 'Export History', href: '/reports/history', icon: DocumentChartBarIcon },
    ],
  },
  {
    title: 'Organization',
    icon: BuildingOfficeIcon,
    items: [
      { name: 'Departments', href: '/organization/departments', icon: BuildingOfficeIcon },
      { name: 'Locations', href: '/organization/locations', icon: BuildingOfficeIcon },
      { name: 'Shifts', href: '/organization/shifts', icon: ClockIcon },
      { name: 'Shift Assignment', href: '/organization/shift-assignment', icon: UsersIcon },
    ],
  },
  {
    title: 'System',
    icon: Cog6ToothIcon,
    items: [
      { name: 'Settings', href: '/settings', icon: Cog6ToothIcon },
      { name: 'Overtime Policies', href: '/settings/overtime', icon: ClockIcon },
      { name: 'System Health', href: '/settings/health', icon: DocumentChartBarIcon },
      { name: 'Audit Logs', href: '/settings/audit', icon: DocumentChartBarIcon },
    ],
  },
]

/**
 * Staff self-service navigation (limited access)
 */
export const staffNavigation: NavigationGroup[] = [
  {
    title: 'My Portal',
    icon: UserCircleIcon,
    items: [
      { name: 'Dashboard', href: '/staff-portal', icon: HomeIcon },
      { name: 'My Attendance', href: '/staff-portal/attendance', icon: ClockIcon },
      { name: 'My Schedule', href: '/staff-portal/schedule', icon: CalendarDaysIcon },
      { name: 'My Fingerprints', href: '/enrollment/self-service', icon: FingerPrintIcon },
    ],
  },
]
