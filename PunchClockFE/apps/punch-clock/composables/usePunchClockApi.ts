/**
 * Punch Clock API Client Composable
 * Provides a typed API client for the Punch Clock backend with auth token management
 */
import { useAuthStore } from '~/stores/auth'
import { PunchClockApiClient } from '~/lib/punch-clock-api'
import type { 
  Staff, 
  Device, 
  AttendanceRecord, 
  PunchLog, 
  LeaveRequest, 
  Department, 
  Location,
  Shift,
  User
} from '~/lib/punch-clock-api'

// Re-export types for convenience
export type {
  Staff,
  Device,
  AttendanceRecord,
  PunchLog,
  LeaveRequest,
  Department,
  Location,
  Shift,
  User
}

export const usePunchClockApi = () => {
  const config = useRuntimeConfig()
  const authStore = useAuthStore()
  
  // Create a custom fetch function that includes auth token
  const authenticatedFetch = async (url: RequestInfo, init?: RequestInit): Promise<Response> => {
    const headers = new Headers(init?.headers)
    
    // Add auth token if available
    if (authStore.token) {
      headers.set('Authorization', `Bearer ${authStore.token}`)
    }
    
    const response = await fetch(url, {
      ...init,
      headers,
    })
    
    // Handle 401 Unauthorized - redirect to login
    if (response.status === 401) {
      authStore.logout()
      throw new Error('Unauthorized - please login again')
    }
    
    return response
  }
  
  // Create client instance with base URL and authenticated fetch
  // Note: baseUrl should be empty string because the generated API endpoints already include "/api/"
  const client = new PunchClockApiClient('', {
    fetch: authenticatedFetch
  })
  
  return client
}
