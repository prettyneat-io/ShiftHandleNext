// Usage Example for Generated TypeScript API Client
// Import the generated client and types
import { 
  PunchClockApiClient, 
  LoginRequest, 
  Staff, 
  AttendanceRecord,
  PunchLog 
} from './punch-clock-api';

// Initialize the API client
const apiClient = new PunchClockApiClient('http://localhost:5187');

// Example: Authentication and basic usage
async function exampleUsage() {
  try {
    // 1. Login to get authentication token
    const loginRequest: LoginRequest = {
      username: 'admin',
      password: 'your-password'
    };

    console.log('Logging in...');
    const loginResponse = await apiClient.login(loginRequest);
    console.log('Login successful!');

    // 2. Get current user info
    const currentUser = await apiClient.me();
    console.log('Current user:', currentUser);

    // 3. Get all staff (with pagination)
    const staffResponse = await apiClient.staffGET(
      1,     // page
      10,    // limit
      'lastName', // sort
      'asc', // order
      undefined, // include
      true   // isActive
    );
    console.log('Staff members:', staffResponse);

    // 4. Get attendance records for the current month
    const startDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1);
    const endDate = new Date();
    
    const attendanceResponse = await apiClient.records(
      startDate,
      endDate,
      undefined, // staffId (get all staff)
      1,         // page
      50         // limit
    );
    console.log('Attendance records:', attendanceResponse);

    // 5. Get punch logs for today
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    const punchLogsResponse = await apiClient.logsGET(
      today,
      tomorrow,
      undefined, // staffId
      undefined, // deviceId
      1,         // page
      100        // limit
    );
    console.log('Today\'s punch logs:', punchLogsResponse);

    // 6. Get devices
    const devicesResponse = await apiClient.devicesGET(
      1,    // page
      20,   // limit
      undefined, // sort
      undefined, // order
      undefined, // include
      true  // isActive
    );
    console.log('Active devices:', devicesResponse);

  } catch (error) {
    console.error('API Error:', error);
  }
}

// Example: Creating new records
async function createExamples() {
  try {
    // Create a new staff member
    const newStaff: Partial<Staff> = {
      employeeId: 'EMP001',
      firstName: 'John',
      lastName: 'Doe',
      email: 'john.doe@company.com',
      phone: '+1234567890',
      positionTitle: 'Developer',
      employmentType: 'FULL_TIME',
      hireDate: new Date(),
      isActive: true
    };

    const createdStaff = await apiClient.staffPOST(newStaff);
    console.log('Created staff:', createdStaff);

    // Create a manual punch log entry
    const newPunchLog: Partial<PunchLog> = {
      staffId: createdStaff.staffId, // Use the newly created staff ID
      punchTime: new Date(),
      punchType: 'IN',
      verificationMode: 'MANUAL',
      isManualEntry: true,
      manualEntryReason: 'Forgot to punch in',
      isValid: true
    };

    const createdPunchLog = await apiClient.logsPOST(newPunchLog);
    console.log('Created punch log:', createdPunchLog);

  } catch (error) {
    console.error('Creation Error:', error);
  }
}

// Example: Error handling with proper TypeScript types
async function handleErrorsExample() {
  try {
    // This will throw an error if staff not found
    const nonExistentStaff = await apiClient.staff2GET('00000000-0000-0000-0000-000000000000');
    console.log(nonExistentStaff);
  } catch (error) {
    // The error will be properly typed based on your API responses
    console.error('Staff not found:', error);
  }
}

// Example: Working with AbortSignal for cancellable requests
async function cancellableRequestExample() {
  const controller = new AbortController();
  
  // Cancel the request after 5 seconds
  setTimeout(() => controller.abort(), 5000);
  
  try {
    const response = await apiClient.staffGET(
      1, 10, undefined, undefined, undefined, true,
      controller.signal // Pass the abort signal
    );
    console.log('Response:', response);
  } catch (error) {
    if (error.name === 'AbortError') {
      console.log('Request was cancelled');
    } else {
      console.error('Request failed:', error);
    }
  }
}

// Example: Batch operations
async function batchOperationsExample() {
  try {
    // Get attendance corrections that need approval
    const correctionsResponse = await apiClient.correctionsGET(
      undefined, // staffId
      'PENDING', // status
      1,         // page
      50         // limit
    );

    console.log('Pending corrections:', correctionsResponse);

    // Bulk approve corrections (if any exist)
    if (correctionsResponse && correctionsResponse.length > 0) {
      const correctionIds = correctionsResponse.map(c => c.correctionId);
      
      const bulkRequest = {
        correctionIds,
        action: 'APPROVE',
        notes: 'Bulk approval via API'
      };

      const bulkResult = await apiClient.bulkApprove(bulkRequest);
      console.log('Bulk approval result:', bulkResult);
    }

  } catch (error) {
    console.error('Batch operation error:', error);
  }
}

// Run examples
console.log('🚀 PunchClock API Client Examples');
console.log('==================================');

// Uncomment the examples you want to run:
// exampleUsage();
// createExamples();
// handleErrorsExample();
// cancellableRequestExample();
// batchOperationsExample();

export { 
  PunchClockApiClient,
  exampleUsage,
  createExamples,
  handleErrorsExample,
  cancellableRequestExample,
  batchOperationsExample
};