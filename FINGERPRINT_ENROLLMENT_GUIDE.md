# Fingerprint Enrollment API Guide

## Overview
This guide explains how to remotely trigger the fingerprint enrollment process on ZKTeco devices via the API.

## How It Works

The `enroll_user` function in PyZK sends a `CMD_STARTENROLL` command to the device, which:

1. **Puts the device into enrollment mode** for a specific user and finger
2. **Prompts the user at the device** to scan their finger **3 times**
3. **Waits up to 60 seconds** for the enrollment process to complete
4. **Returns success/failure** based on whether all 3 scans were captured successfully

## API Endpoint

### Enroll Fingerprint

```http
POST /api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId={0-9}
```

**Path Parameters:**
- `deviceId` (Guid) - The ID of the ZKTeco device
- `staffId` (Guid) - The ID of the staff member to enroll

**Query Parameters:**
- `fingerId` (int, optional, default: 0) - Finger index (0-9)
  - 0 = Right Thumb
  - 1 = Right Index
  - 2 = Right Middle
  - 3 = Right Ring
  - 4 = Right Pinky
  - 5 = Left Thumb
  - 6 = Left Index
  - 7 = Left Middle
  - 8 = Left Ring
  - 9 = Left Pinky

**Response (Success):**
```json
{
  "success": true,
  "message": "Enrollment completed successfully",
  "deviceId": "550e8400-e29b-41d4-a716-446655440000",
  "staffId": "660e8400-e29b-41d4-a716-446655440001",
  "fingerId": 0,
  "instructions": "User should scan their finger on the device 3 times when prompted"
}
```

**Response (Failure):**
```json
{
  "success": false,
  "error": "Enrollment failed or timed out",
  "deviceId": "550e8400-e29b-41d4-a716-446655440000",
  "staffId": "660e8400-e29b-41d4-a716-446655440001",
  "fingerId": 0
}
```

## Usage Examples

### Using cURL

```bash
# Enroll right index finger (finger ID 1)
curl -X POST "http://localhost:5187/api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId=1" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Enroll default finger (right thumb, finger ID 0)
curl -X POST "http://localhost:5187/api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Using JavaScript/Fetch

```javascript
async function enrollFingerprint(deviceId, staffId, fingerId = 0) {
  const response = await fetch(
    `http://localhost:5187/api/devices/${deviceId}/staff/${staffId}/enroll-fingerprint?fingerId=${fingerId}`,
    {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    }
  );
  
  const result = await response.json();
  
  if (result.success) {
    console.log('✅ Enrollment started!');
    console.log('👉 Tell the user to scan their finger 3 times on the device');
    return result;
  } else {
    console.error('❌ Enrollment failed:', result.error);
    throw new Error(result.error);
  }
}

// Example usage
try {
  await enrollFingerprint(
    '550e8400-e29b-41d4-a716-446655440000',  // deviceId
    '660e8400-e29b-41d4-a716-446655440001',  // staffId
    1  // Right Index finger
  );
} catch (error) {
  console.error('Failed to enroll:', error);
}
```

### Using C# HttpClient

```csharp
public async Task<bool> EnrollFingerprintAsync(
    Guid deviceId, 
    Guid staffId, 
    int fingerId = 0)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.Authorization = 
        new AuthenticationHeaderValue("Bearer", token);
    
    var url = $"http://localhost:5187/api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId={fingerId}";
    
    var response = await client.PostAsync(url, null);
    
    if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadFromJsonAsync<EnrollmentResult>();
        Console.WriteLine($"✅ {result.Message}");
        Console.WriteLine("👉 User should scan finger 3 times on device");
        return true;
    }
    else
    {
        var error = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"❌ Enrollment failed: {error}");
        return false;
    }
}
```

## Workflow Integration

### Typical Enrollment Flow

1. **Add user to device** (if not already enrolled)
   ```http
   POST /api/devices/{deviceId}/staff/{staffId}/enroll
   ```

2. **Start fingerprint enrollment** (can enroll multiple fingers)
   ```http
   POST /api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId=0
   ```
   - Device will beep and display prompts
   - User scans their right thumb 3 times
   - API returns success after 3rd scan completes

3. **Repeat for additional fingers** (optional)
   ```http
   POST /api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId=1
   POST /api/devices/{deviceId}/staff/{staffId}/enroll-fingerprint?fingerId=2
   ```

4. **Verify enrollment**
   ```http
   GET /api/devices/{deviceId}/users
   ```
   - Check that user appears with fingerprint templates

## Important Notes

### Prerequisites
- ✅ **User must exist on device** - The endpoint automatically creates the user if needed
- ✅ **Device must be online** - The API checks connectivity automatically
- ✅ **Network connection** - Device must be reachable from the API server

### Timing Considerations
- ⏱️ **60-second timeout** - The enrollment process waits up to 60 seconds
- ⏱️ **User must be present** - Someone must be at the device to scan their finger
- ⏱️ **Immediate action required** - Start the process only when user is ready

### Best Practices

1. **Notify the user first**
   ```javascript
   // Bad: Surprise enrollment
   await enrollFingerprint(deviceId, staffId, 0);
   
   // Good: Notify user before starting
   alert("Please go to the device and be ready to scan your finger 3 times");
   setTimeout(() => {
     enrollFingerprint(deviceId, staffId, 0);
   }, 5000); // Give user 5 seconds to get to device
   ```

2. **Enroll multiple fingers** for redundancy
   ```javascript
   // Enroll 3 fingers for best reliability
   await enrollFingerprint(deviceId, staffId, 0);  // Right thumb
   await enrollFingerprint(deviceId, staffId, 1);  // Right index
   await enrollFingerprint(deviceId, staffId, 5);  // Left thumb
   ```

3. **Handle timeout gracefully**
   ```javascript
   try {
     await enrollFingerprint(deviceId, staffId, 0);
   } catch (error) {
     if (error.message.includes('timeout')) {
       alert('Enrollment timed out. Please try again and scan your finger immediately when prompted.');
     }
   }
   ```

4. **Validate enrollment success**
   ```javascript
   // After enrollment, verify the template exists
   const users = await fetch(`/api/devices/${deviceId}/users`).then(r => r.json());
   const user = users.find(u => u.userId === staffEmployeeId);
   
   if (!user || !user.templates || user.templates.length === 0) {
     console.error('Enrollment completed but no templates found');
   }
   ```

## Troubleshooting

### "User not found"
- Ensure staff member exists in database
- Check that `staffId` is valid

### "Device not found"
- Verify `deviceId` is correct
- Ensure device is registered in system

### "Failed to connect"
- Check device IP address and port
- Verify device is powered on and connected to network
- Test connection: `POST /api/devices/{deviceId}/test-connection`

### "Enrollment failed"
Common causes:
- User didn't scan finger 3 times
- Finger scans were inconsistent
- Device timeout (60 seconds expired)
- Device was busy with another operation

### "Finger ID must be between 0 and 9"
- Use finger index 0-9 only
- Each index represents a specific finger (see parameters above)

## Database Changes

When enrollment succeeds, the API automatically:
1. Updates `staff.enrollment_status` to "COMPLETED"
2. Creates/updates record in `biometric_templates` table:
   - `template_type` = "FINGERPRINT"
   - `finger_index` = the specified finger ID
   - `enrolled_at` = current timestamp
   - `is_active` = true

## Related Endpoints

- **Add user to device:** `POST /api/devices/{deviceId}/staff/{staffId}/enroll`
- **Get device users:** `GET /api/devices/{deviceId}/users`
- **Test connection:** `POST /api/devices/{deviceId}/test-connection`
- **Get device info:** `GET /api/devices/{deviceId}/info`
- **Sync attendance:** `POST /api/devices/{deviceId}/sync?type=attendance`

## Support

For issues or questions, refer to:
- PyZK documentation: Device-specific enrollment behavior
- ZKTeco device manuals: Fingerprint quality requirements
- API logs: Check `ILogger<DeviceService>` output for detailed error messages
