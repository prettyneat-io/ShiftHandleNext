# PunchClock API TypeScript Client

This directory contains the auto-generated TypeScript client for the PunchClock API, created using NSwag.

## 🎉 Generated Successfully!

✅ **7,178 lines** of TypeScript code generated from your API  
✅ **Full type safety** with all your models and endpoints  
✅ **Zero manual coding** - automatically synced with your C# API  

## 📁 Files

- `punch-clock-api.ts` - Main generated client (7,178 lines)
- `working-examples.ts` - Usage examples
- `README.md` - This file

## 🚀 Quick Start

### 1. Install in your frontend project:

```bash
# Copy the generated file to your frontend project
cp punch-clock-api.ts /path/to/your/frontend/src/api/

# Or create a symbolic link to always stay in sync
ln -s $(pwd)/punch-clock-api.ts /path/to/your/frontend/src/api/
```

### 2. Basic usage:

```typescript
import { PunchClockApiClient, LoginRequest } from './api/punch-clock-api';

// Initialize client
const apiClient = new PunchClockApiClient('http://localhost:5187');

// Login
const loginRequest = new LoginRequest({
  username: 'admin',
  password: 'password'
});

await apiClient.login(loginRequest);

// Get staff
const staff = await apiClient.staffGET(1, 10, 'lastName', 'asc', undefined, true);
```

## 🔄 Auto-Regeneration

To regenerate the client when your API changes:

```bash
# From the PunchClockApi directory
nswag openapi2tsclient /input:http://localhost:5187/swagger/v1/swagger.json /output:./generated/punch-clock-api.ts /className:PunchClockApiClient /template:Fetch /promiseType:Promise /dateTimeType:Date /generateOptionalParameters:true /useAbortSignal:true /exportTypes:true
```

Or use the provided script:
```bash
./generate-ts-client.sh
```

## 📋 Available Methods

The generated client includes methods for all your controllers:

### Authentication (`/api/auth`)
- `login(request)` - User login
- `register(request)` - User registration  
- `refresh(request)` - Token refresh
- `logout()` - User logout
- `me()` - Get current user

### Staff Management (`/api/staff`)
- `staffGET()` - Get all staff with filtering/pagination
- `staff2GET(id)` - Get staff by ID
- `staffPOST(staff)` - Create new staff
- `staffPUT(id, staff)` - Update staff
- `staffDELETE(id)` - Soft delete staff
- Export/import methods for CSV operations

### Attendance (`/api/attendance`)
- `logsGET()` - Get punch logs with filtering
- `logsPOST(log)` - Create manual punch log
- `records()` - Get attendance records
- `correctionsGET()` - Get corrections
- `correctionsPOST(correction)` - Create correction
- `approve(id)` / `reject(id)` - Approve/reject corrections
- `bulkApprove()` - Bulk operations

### Devices (`/api/devices`)
- `devicesGET()` - Get devices with filtering
- `devicesPOST()` - Create device
- `devicesPUT(id)` - Update device
- `sync(id)` - Sync device data
- `connect(id)` / `disconnect(id)` - Device connection
- Enrollment methods

### Leave Management (`/api/leave`)
- `typesGET()` - Get leave types
- `typesPOST()` - Create leave type
- `requestsGET()` - Get leave requests
- `requestsPOST()` - Create leave request
- Approval/cancellation methods

### And more...
- Organization management
- User management  
- Reports
- System settings
- Overtime policies

## 🎯 Key Features

### ✅ Type Safety
Every request and response is fully typed:
```typescript
const staff: Staff = await apiClient.staff2GET(staffId);
// staff.firstName is string
// staff.isActive is boolean  
// staff.createdAt is Date
```

### ✅ Request Cancellation
```typescript
const controller = new AbortController();
setTimeout(() => controller.abort(), 5000);

await apiClient.staffGET(1, 10, undefined, undefined, undefined, true, controller.signal);
```

### ✅ Error Handling
Proper error responses with types:
```typescript
try {
  await apiClient.staffGET();
} catch (error) {
  // Error includes status code, message, etc.
  console.error('API Error:', error);
}
```

### ✅ Automatic Serialization
Complex objects are automatically serialized:
```typescript
const loginRequest = new LoginRequest({
  username: 'admin',
  password: 'secret'
});

// Automatically converted to proper JSON
await apiClient.login(loginRequest);
```

## 🔧 Integration Examples

### React/Next.js
```typescript
import { PunchClockApiClient } from '../api/punch-clock-api';

const useApiClient = () => {
  const client = new PunchClockApiClient('/api');
  
  // Set auth token from your auth state
  const setAuthToken = (token: string) => {
    // You'll need to extend the client or use a wrapper
  };
  
  return { client, setAuthToken };
};
```

### Vue/Nuxt
```typescript
// plugins/api-client.ts
import { PunchClockApiClient } from '../api/punch-clock-api';

export default defineNuxtPlugin(() => {
  const client = new PunchClockApiClient('/api');
  
  return {
    provide: {
      apiClient: client
    }
  };
});
```

### Angular
```typescript
@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private client = new PunchClockApiClient('/api');
  
  async getStaff() {
    return this.client.staffGET(1, 10, 'lastName', 'asc', undefined, true);
  }
}
```

## 🎛️ Configuration Options

When regenerating, you can customize:

- `className` - Name of the generated client class
- `template` - Template type (Fetch, Angular, etc.)
- `dateTimeType` - Date handling (Date, string, moment, etc.)  
- `promiseType` - Promise/Observable types
- `useAbortSignal` - Request cancellation support

## 🔄 Keeping in Sync

### Option 1: Manual regeneration
Run the nswag command when API changes

### Option 2: Build integration  
Add to your build process to auto-regenerate

### Option 3: CI/CD integration
Generate in your deployment pipeline

### Option 4: File watcher
Use a file watcher to regenerate on C# file changes

## 🐛 Troubleshooting

### Client not found
Make sure your API is running on `http://localhost:5187` when generating

### Type errors
The generated client expects specific data structures - check your API responses

### Authentication issues  
The client doesn't handle token storage automatically - you'll need to wrap it or extend it

### CORS issues
Make sure your API has CORS configured for your frontend domain

---

**Generated with NSwag v14.6.2.0**  
**Auto-generated from PunchClock API on 2025-11-08**