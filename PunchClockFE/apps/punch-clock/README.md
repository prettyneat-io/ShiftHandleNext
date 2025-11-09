# Punch Clock Frontend Application

## Overview
A comprehensive punch clock and attendance management system built with Nuxt 3, integrating with the PunchClockAPI backend.

## Backend Integration

### API Configuration
The application connects to the backend API at `http://localhost:5187` (configurable via environment variables).

**Configuration Location:** `nuxt.config.ts`

```typescript
vite: {
  server: {
    proxy: {
      '/api': {
        target: process.env.BACKEND_URL || 'http://localhost:5187',
        changeOrigin: true,
        secure: false,
        ws: true, // WebSocket support for SignalR
      },
    },
  },
}
```

### Environment Variables
- `BACKEND_URL`: Backend API URL (default: `http://localhost:5187`)
- `NUXT_PUBLIC_API_BASE`: Public API base path (default: `/api`)

### API Client

**Location:** `lib/punch-clock-api.ts`

Auto-generated TypeScript client from the backend OpenAPI specification using NSwag. Provides type-safe access to all API endpoints.

**Usage:**
```typescript
import { usePunchClockApi } from '~/composables/usePunchClockApi'

const api = usePunchClockApi()
const staff = await api.getAllStaffStaff()
```

### Dashboard Data

**Composable:** `composables/useDashboard.ts`

Fetches real-time dashboard statistics from the following endpoints:

- **GET** `/api/staff` - Total active staff count
- **GET** `/api/attendance/records` - Today's attendance records
  - Filters for clocked-in staff
  - Calculates late arrivals (lateMinutes > 0)
- **GET** `/api/attendance/corrections` - Pending correction requests
- **GET** `/api/devices` - Device status and sync information
- **GET** `/api/leave/requests` - Pending leave requests
- **GET** `/api/reports/summary-statistics` - Daily summary statistics
- **GET** `/api/system/health` - System health check

### Features

#### Auto-Refresh
The dashboard automatically refreshes every 30 seconds to display live data. This can be toggled on/off using the auto-refresh checkbox.

#### Manual Refresh
Click the refresh button in the top-right corner to manually update dashboard data.

#### Real-Time Clock
Live clock display updates every second showing current date and time.

#### Statistics Cards
- **Total Staff**: Active staff count
- **Clocked In Today**: Number of staff with clock-in records today
- **Late Arrivals**: Staff who arrived late (lateMinutes > 0)
- **Pending Corrections**: Attendance corrections awaiting approval

#### System Status
- **API Status**: Backend health check status
- **Active Devices**: Connected biometric devices count
- **Last Sync**: Most recent device synchronization time

## Development

### Prerequisites
- Node.js 18+
- pnpm
- Backend API running at `http://localhost:5187`

### Setup
```bash
# Install dependencies
pnpm install

# Run development server
pnpm run dev
```

### Building
```bash
pnpm run build
```

## API Endpoints Reference

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `POST /api/auth/refresh` - Refresh access token
- `GET /api/auth/me` - Get current user

### Staff Management
- `GET /api/staff` - List all staff
- `POST /api/staff` - Create staff member
- `GET /api/staff/{id}` - Get staff details
- `PUT /api/staff/{id}` - Update staff member
- `DELETE /api/staff/{id}` - Delete staff member

### Attendance
- `GET /api/attendance/logs` - Get punch logs
- `POST /api/attendance/logs` - Create punch log
- `GET /api/attendance/records` - Get attendance records
- `GET /api/attendance/corrections` - Get correction requests
- `POST /api/attendance/corrections` - Submit correction request

### Devices
- `GET /api/devices` - List all devices
- `POST /api/devices` - Register new device
- `GET /api/devices/{id}` - Get device details
- `POST /api/devices/{id}/sync` - Sync device data
- `POST /api/devices/{id}/connect` - Connect to device
- `POST /api/devices/{id}/disconnect` - Disconnect from device

### Leave Management
- `GET /api/leave/types` - List leave types
- `GET /api/leave/requests` - List leave requests
- `POST /api/leave/requests` - Submit leave request
- `POST /api/leave/requests/{id}/approve` - Approve leave request
- `POST /api/leave/requests/{id}/reject` - Reject leave request

### Reports
- `GET /api/reports/daily` - Daily attendance report
- `GET /api/reports/monthly` - Monthly attendance report
- `GET /api/reports/payroll` - Payroll report
- `GET /api/reports/summary-statistics` - Summary statistics

### System
- `GET /api/system/health` - Health check
- `GET /api/system/settings` - Get system settings
- `PUT /api/system/settings` - Update system settings

## Troubleshooting

### Backend Connection Issues
1. Ensure the backend is running at `http://localhost:5187`
2. Check the Vite proxy configuration in `nuxt.config.ts`
3. Verify CORS settings on the backend
4. Check browser console for network errors

### Authentication Issues
1. Clear browser localStorage
2. Check token expiration
3. Verify authentication endpoints are accessible
4. Review auth middleware configuration

### Data Not Loading
1. Check network tab in browser dev tools
2. Verify API endpoints return expected response format
3. Review error messages in console
4. Check loading states in Vue DevTools

## Type Safety

All API endpoints are fully typed using TypeScript interfaces generated from the backend OpenAPI specification. This ensures compile-time type checking and excellent IDE autocomplete support.

## Error Handling

The application includes comprehensive error handling:
- Network errors are caught and displayed to users
- 401 Unauthorized responses trigger automatic logout
- Failed API calls show user-friendly error messages
- Loading states prevent duplicate requests
