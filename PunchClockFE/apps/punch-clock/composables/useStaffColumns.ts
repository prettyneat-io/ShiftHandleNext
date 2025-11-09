import type { Col } from '../../../layers/shared/types/Column'

/**
 * Composable to provide Staff column definitions derived from the Staff type in punch-clock-api.ts
 * 
 * This replaces the previous validation-meta.json approach and ensures columns are
 * always in sync with the actual API types.
 */
export const useStaffColumns = () => {
  /**
   * Columns for the Staff list view (index page)
   * Includes all fields from the Staff interface
   */
  const listColumns: Col[] = [
    { 
      key: 'badgeNumber', 
      label: 'Badge Number', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'employeeId', 
      label: 'Employee ID', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'firstName', 
      label: 'First Name', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'middleName', 
      label: 'Middle Name', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'lastName', 
      label: 'Last Name', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'email', 
      label: 'Email', 
      type: 'email', 
      sortable: true 
    },
    { 
      key: 'phone', 
      label: 'Phone', 
      type: 'text',
      sortable: true 
    },
    { 
      key: 'mobile', 
      label: 'Mobile', 
      type: 'text',
      sortable: true 
    },
    { 
      key: 'department.departmentName', 
      label: 'Department', 
      type: 'text', 
      displayPath: 'department.departmentName', 
      include: 'department', 
      sortable: true 
    },
    { 
      key: 'location.locationName', 
      label: 'Location', 
      type: 'text', 
      displayPath: 'location.locationName', 
      include: 'location', 
      sortable: true 
    },
    { 
      key: 'shift.shiftName', 
      label: 'Shift', 
      type: 'text', 
      displayPath: 'shift.shiftName', 
      include: 'shift', 
      sortable: true 
    },
    { 
      key: 'positionTitle', 
      label: 'Position', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'employmentType', 
      label: 'Employment Type', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'hireDate', 
      label: 'Hire Date', 
      type: 'date', 
      sortable: true 
    },
    { 
      key: 'terminationDate', 
      label: 'Termination Date', 
      type: 'date', 
      sortable: true 
    },
    { 
      key: 'isActive', 
      label: 'Status', 
      type: 'boolean',
      sortable: true 
    },
    { 
      key: 'enrollmentStatus', 
      label: 'Enrollment', 
      type: 'text',
      sortable: true 
    },
    { 
      key: 'createdAt', 
      label: 'Created At', 
      type: 'date', 
      sortable: true 
    },
    { 
      key: 'updatedAt', 
      label: 'Updated At', 
      type: 'date', 
      sortable: true 
    },
    { 
      key: 'createdBy', 
      label: 'Created By', 
      type: 'text', 
      sortable: true 
    },
    { 
      key: 'updatedBy', 
      label: 'Updated By', 
      type: 'text', 
      sortable: true 
    },
  ]

  /**
   * Form fields for creating/editing staff
   * Includes all editable fields from the Staff interface
   */
  const formFields: Col[] = [
    { 
      key: 'employeeId', 
      label: 'Employee ID', 
      type: 'text',
      required: false
    },
    { 
      key: 'badgeNumber', 
      label: 'Badge Number', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'firstName', 
      label: 'First Name', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'middleName', 
      label: 'Middle Name', 
      type: 'text',
      required: false
    },
    { 
      key: 'lastName', 
      label: 'Last Name', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'email', 
      label: 'Email', 
      type: 'email', 
      required: true 
    },
    { 
      key: 'phone', 
      label: 'Phone', 
      type: 'text',
      required: false
    },
    { 
      key: 'mobile', 
      label: 'Mobile', 
      type: 'text',
      required: false
    },
    { 
      key: 'departmentId', 
      label: 'Department', 
      type: 'select', 
      endpoint: '/api/departments', 
      displayPath: 'departmentName', 
      valuePath: 'departmentId',
      required: false
    },
    { 
      key: 'locationId', 
      label: 'Location', 
      type: 'select', 
      endpoint: '/api/locations', 
      displayPath: 'locationName', 
      valuePath: 'locationId',
      required: false
    },
    { 
      key: 'shiftId', 
      label: 'Shift', 
      type: 'select', 
      endpoint: '/api/shifts', 
      displayPath: 'shiftName', 
      valuePath: 'shiftId',
      required: false
    },
    { 
      key: 'positionTitle', 
      label: 'Position Title', 
      type: 'text',
      required: false
    },
    { 
      key: 'employmentType', 
      label: 'Employment Type', 
      type: 'select', 
      options: ['Full-Time', 'Part-Time', 'Contract', 'Temporary', 'Intern'],
      required: false
    },
    { 
      key: 'hireDate', 
      label: 'Hire Date', 
      type: 'date',
      required: false
    },
    { 
      key: 'terminationDate', 
      label: 'Termination Date', 
      type: 'date',
      required: false
    },
    { 
      key: 'isActive', 
      label: 'Active', 
      type: 'checkbox', 
      defaultValue: true,
      required: false
    },
    { 
      key: 'enrollmentStatus', 
      label: 'Enrollment Status', 
      type: 'select',
      options: ['Not Enrolled', 'Pending', 'Enrolled', 'Failed'],
      required: false
    },
  ]

  /**
   * Minimal form fields for quick staff creation
   */
  const quickCreateFields: Col[] = [
    { 
      key: 'badgeNumber', 
      label: 'Badge Number', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'firstName', 
      label: 'First Name', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'lastName', 
      label: 'Last Name', 
      type: 'text', 
      required: true 
    },
    { 
      key: 'email', 
      label: 'Email', 
      type: 'email', 
      required: true 
    },
    { 
      key: 'phone', 
      label: 'Phone', 
      type: 'text',
      required: false
    },
    { 
      key: 'departmentId', 
      label: 'Department', 
      type: 'select', 
      endpoint: '/api/departments', 
      displayPath: 'departmentName', 
      valuePath: 'departmentId',
      required: false
    },
    { 
      key: 'positionTitle', 
      label: 'Position', 
      type: 'text',
      required: false
    },
    { 
      key: 'hireDate', 
      label: 'Hire Date', 
      type: 'date',
      required: false
    },
    { 
      key: 'isActive', 
      label: 'Active', 
      type: 'checkbox', 
      defaultValue: true,
      required: false
    },
  ]

  return {
    listColumns,
    formFields,
    quickCreateFields
  }
}
