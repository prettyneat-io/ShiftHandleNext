// Working Usage Examples for NSwag Generated TypeScript API Client

import { 
  PunchClockApiClient, 
  LoginRequest, 
  Staff, 
  PunchLog 
} from './punch-clock-api';

// Initialize the API client
const apiClient = new PunchClockApiClient('http://localhost:5187');

// Example 1: Basic authentication and user info
async function authenticationExample() {
  try {
    console.log('🔐 Authentication Example');
    
    // Create login request using the generated class
    const loginRequest = new LoginRequest({
      username: 'admin',
      password: 'your-password'
    });

    console.log('Logging in...');
    await apiClient.login(loginRequest);
    console.log('✅ Login successful!');

    // Get current user info
    await apiClient.me();
    console.log('✅ Got current user info');

  } catch (error) {
    console.error('❌ Authentication error:', error);
  }
}

// Example 2: Working with staff data
async function staffExample() {
  try {
    console.log('\n👥 Staff Management Example');
    
    // Get all active staff with pagination
    console.log('Fetching staff...');
    await apiClient.staffGET(
      1,     // page
      10,    // limit
      'lastName', // sort
      'asc', // order
      undefined, // include
      true   // isActive
    );
    console.log('✅ Fetched staff list');

    // Get specific staff by ID (replace with real ID)
    // await apiClient.staff2GET('staff-id-here');

  } catch (error) {
    console.error('❌ Staff error:', error);
  }
}

// Example 3: Working with attendance data
async function attendanceExample() {
  try {
    console.log('\n📊 Attendance Example');
    
    // Get today's punch logs
    const today = new Date();
    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);
    
    console.log('Fetching punch logs...');
    await apiClient.logsGET(
      today,     // startDate
      tomorrow,  // endDate
      undefined, // staffId
      undefined, // deviceId
      1,         // page
      50,        // limit
      'punchTime', // sort
      'desc'     // order
    );
    console.log('✅ Fetched punch logs');

    // Get attendance records for current month
    const startOfMonth = new Date(today.getFullYear(), today.getMonth(), 1);
    console.log('Fetching attendance records...');
    await apiClient.records(
      startOfMonth,
      today,
      undefined, // staffId
      1,         // page
      50         // limit
    );
    console.log('✅ Fetched attendance records');

  } catch (error) {
    console.error('❌ Attendance error:', error);
  }
}

// Example 4: Working with devices
async function devicesExample() {
  try {
    console.log('\n🔧 Devices Example');
    
    console.log('Fetching devices...');
    await apiClient.devicesGET(
      1,    // page
      20,   // limit
      'deviceName', // sort
      'asc', // order
      undefined, // include
      true  // isActive
    );
    console.log('✅ Fetched devices');

  } catch (error) {
    console.error('❌ Devices error:', error);
  }
}

// Example 5: Working with leave management
async function leaveExample() {
  try {
    console.log('\n🏖️ Leave Management Example');
    
    // Get leave types
    console.log('Fetching leave types...');
    await apiClient.typesGET(true); // isActive = true
    console.log('✅ Fetched leave types');

    // Get leave requests
    console.log('Fetching leave requests...');
    await apiClient.requestsGET(
      undefined, // staffId
      undefined, // leaveTypeId
      'PENDING', // status
      undefined, // startDate
      undefined, // endDate
      1,         // page
      20         // limit
    );
    console.log('✅ Fetched leave requests');

  } catch (error) {
    console.error('❌ Leave management error:', error);
  }
}

// Example 6: Error handling
async function errorHandlingExample() {
  try {
    console.log('\n⚠️ Error Handling Example');
    
    // This will likely fail with 401 if not authenticated
    await apiClient.staffGET(1, 10);
    
  } catch (error: any) {
    console.log('✅ Caught expected error:', error.message || error);
  }
}

// Example 7: Request cancellation
async function cancellationExample() {
  try {
    console.log('\n⏹️ Request Cancellation Example');
    
    const controller = new AbortController();
    
    // Cancel after 1 second
    setTimeout(() => {
      controller.abort();
      console.log('🚫 Request cancelled');
    }, 1000);
    
    await apiClient.staffGET(
      1, 10, undefined, undefined, undefined, true,
      controller.signal
    );
    
  } catch (error: any) {
    if (error.name === 'AbortError') {
      console.log('✅ Request was successfully cancelled');
    } else {
      console.error('❌ Unexpected error:', error);
    }
  }
}

// Main execution function
async function runExamples() {
  console.log('🚀 PunchClock API Client Examples');
  console.log('==================================');

  // Run examples (uncomment the ones you want to test)
  
  // Basic examples (these should work without authentication)
  await errorHandlingExample();
  await cancellationExample();
  
  // Examples that require authentication (uncomment after setting up credentials)
  // await authenticationExample();
  // await staffExample();
  // await attendanceExample();
  // await devicesExample();
  // await leaveExample();

  console.log('\n✨ Examples completed!');
  console.log('\n💡 To use authenticated endpoints:');
  console.log('1. Update the login credentials in authenticationExample()');
  console.log('2. Call authenticationExample() first');
  console.log('3. Then run the other examples');
  console.log('\n📚 Generated API Documentation:');
  console.log('- Check ./generated/punch-clock-api.ts for all available methods');
  console.log('- All classes and interfaces are fully typed');
  console.log('- AbortSignal support for cancellable requests');
  console.log('- Automatic JSON serialization/deserialization');
}

// Export functions for individual use
export {
  apiClient,
  authenticationExample,
  staffExample,
  attendanceExample,
  devicesExample,
  leaveExample,
  errorHandlingExample,
  cancellationExample,
  runExamples
};

// Run all examples if this file is executed directly
if (typeof window === 'undefined' && require.main === module) {
  runExamples().catch(console.error);
}