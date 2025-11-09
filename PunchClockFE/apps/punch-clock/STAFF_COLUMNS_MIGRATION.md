# Staff Directory Column Migration

## Overview
This document describes the migration from `validation-meta.json`-based column definitions to type-based column definitions for the Staff Directory feature.

## Changes Made

### 1. Created `useStaffColumns` Composable
**File**: `apps/punch-clock/composables/useStaffColumns.ts`

This new composable provides three sets of column definitions:
- `listColumns`: Columns displayed in the staff list view (index page)
- `formFields`: Complete form fields for creating/editing staff
- `quickCreateFields`: Minimal fields for quick staff creation

All column definitions are derived directly from the `Staff` interface in `lib/punch-clock-api.ts`, ensuring:
- Type safety
- Consistency with the API
- No dependency on external metadata files
- Easy maintenance (changes to the API type automatically show as TypeScript errors)

### 2. Updated Staff Pages

#### `pages/staff/index.vue`
- Now uses `listColumns` from `useStaffColumns()`
- Removed hardcoded column definitions
- Added `employeeId` column to match API type

#### `pages/staff/new.vue`
- Now uses `formFields` from `useStaffColumns()`
- Includes all available staff properties from the API
- Added missing fields: `employeeId`, `middleName`, `mobile`, `locationId`, `shiftId`, `enrollmentStatus`

#### `pages/staff/[id]/edit.vue`
- Now uses `formFields` from `useStaffColumns()`
- Fixed notification store usage to use `addNotification` method
- Consistent field set with create form

### 3. Updated Validation Utilities

#### `layers/shared/utils/yup-schema-generator.ts`
- Removed dependency on `validation-meta.json`
- Now generates validation schema directly from `Field` definitions
- Made `endpoint` parameter optional (kept for API compatibility)
- Schema is based on field `type` and `required` properties

#### `layers/shared/utils/form-columns-dynamic.ts`
- Removed dependency on `validation-meta.json`
- Deprecated `fetchColumnsForEndpoint` function with clear warning
- Recommends creating entity-specific composables instead

### 4. Removed Dependencies
- `validation-meta.json` is no longer referenced in staff-related code
- Validation now comes from explicit field definitions

## Benefits

1. **Type Safety**: Column definitions are TypeScript-typed and checked at compile time
2. **Single Source of Truth**: API types in `punch-clock-api.ts` drive the column definitions
3. **Maintainability**: Changes to API types are immediately visible in TypeScript errors
4. **Clarity**: Column definitions are explicit and easy to understand
5. **No Hidden Dependencies**: No reliance on generated JSON files

## Usage Pattern for Other Entities

To create similar column definitions for other entities:

1. Create a composable: `composables/use[Entity]Columns.ts`
2. Import the entity type from `~/lib/punch-clock-api`
3. Define column arrays with proper types from the API
4. Export `listColumns` and `formFields` arrays
5. Use the composable in your pages

Example:
```typescript
import type { Col } from '../../../layers/shared/types/Column'

export const useDepartmentColumns = () => {
  const listColumns: Col[] = [
    { key: 'departmentCode', label: 'Code', type: 'text', sortable: true },
    { key: 'departmentName', label: 'Name', type: 'text', sortable: true },
    // ... more columns
  ]

  const formFields: Col[] = [
    { key: 'departmentCode', label: 'Code', type: 'text', required: true },
    { key: 'departmentName', label: 'Name', type: 'text', required: true },
    // ... more fields
  ]

  return { listColumns, formFields }
}
```

## API Field Mappings

The following fields from the `Staff` interface are now available:

### Basic Information
- `employeeId` (NEW)
- `badgeNumber`
- `firstName`
- `middleName` (NEW)
- `lastName`

### Contact Information
- `email`
- `phone`
- `mobile` (NEW)

### Employment Details
- `departmentId`
- `locationId` (NEW)
- `shiftId` (NEW)
- `positionTitle`
- `employmentType`
- `hireDate`
- `terminationDate`

### Status
- `isActive`
- `enrollmentStatus` (NEW)

### Relations (for display only)
- `department.departmentName`
- `location.locationName`
- `shift.shiftName`

## Testing Checklist

- [ ] List view displays all columns correctly
- [ ] Search and sort functionality works
- [ ] Create form shows all fields
- [ ] Create form validation works
- [ ] Edit form loads existing data
- [ ] Edit form validation works
- [ ] Select fields (department, location, shift) load options
- [ ] Form submission succeeds
- [ ] No console errors related to validation
