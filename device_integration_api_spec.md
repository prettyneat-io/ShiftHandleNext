# Device Integration API Specification
## PYZK API Integration Layer

### Overview
The PYZK API provides a REST interface to communicate with ZKTeco devices. This document outlines how to integrate these device endpoints with our punch clock system's database schema and business logic.

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend Application                      │
└─────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────┐
│                    Main API Service                           │
│                  (Business Logic Layer)                       │
└─────────────────────────────────────────────────────────────┘
                               │
                ┌──────────────┴──────────────┐
                ▼                              ▼
┌──────────────────────────┐    ┌──────────────────────────┐
│     PostgreSQL DB        │    │    PYZK API Service      │
│   (Data Persistence)     │    │  (Device Communication)   │
└──────────────────────────┘    └──────────────────────────┘
                                              │
                                              ▼
                               ┌──────────────────────────┐
                               │   ZKTeco Devices         │
                               │   (Physical Hardware)    │
                               └──────────────────────────┘
```

## PYZK API Endpoints Mapping

### 1. Device Information Management

#### Get Device Info
**PYZK Endpoint**: `GET /device?ip={ip}&port={port}`

**Integration Flow**:
```javascript
async function syncDeviceInfo(deviceId) {
    // 1. Get device connection info from database
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // 2. Call PYZK API
    const response = await pyzkApi.get('/device', {
        params: {
            ip: device.ip_address,
            port: device.port
        },
        headers: {
            'X-Pretty-Key': PYZK_API_KEY
        }
    });
    
    // 3. Update device record in database
    await db.devices.update(deviceId, {
        firmware_version: response.data.firmware_version,
        user_capacity: response.data.user_capacity,
        log_capacity: response.data.log_capacity,
        fingerprint_capacity: response.data.fingerprint_capacity,
        face_capacity: response.data.face_capacity,
        is_online: true,
        last_heartbeat_at: new Date()
    });
    
    return response.data;
}
```

### 2. User/Staff Management

#### Get All Users from Device
**PYZK Endpoint**: `GET /users?ip={ip}&port={port}&fingers={true/false}`

**Integration Flow**:
```javascript
async function syncUsersFromDevice(deviceId) {
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // Get users with fingerprint data
    const response = await pyzkApi.get('/users', {
        params: {
            ip: device.ip_address,
            port: device.port,
            fingers: true
        }
    });
    
    // Process each user
    for (const deviceUser of response.data.users) {
        // Check if user exists in database
        const staff = await db.staff.findOne({ 
            employee_id: deviceUser.user_id 
        });
        
        if (staff) {
            // Update device enrollment status
            await db.device_enrollments.upsert({
                device_id: deviceId,
                staff_id: staff.staff_id,
                device_user_id: deviceUser.user_id,
                enrollment_status: 'ENROLLED',
                last_sync_at: new Date()
            });
            
            // Store fingerprint templates if present
            if (deviceUser.templates) {
                for (const template of deviceUser.templates) {
                    await db.biometric_templates.upsert({
                        staff_id: staff.staff_id,
                        template_type: 'FINGERPRINT',
                        template_data: template.template,
                        finger_index: template.fid,
                        quality_score: template.valid * 100,
                        device_id: deviceId
                    });
                }
            }
        }
    }
}
```

#### Create/Update User on Device
**PYZK Endpoint**: `PUT /user?ip={ip}&port={port}` (Create)
**PYZK Endpoint**: `POST /user?ip={ip}&port={port}` (Update)

**Integration Flow**:
```javascript
async function pushUserToDevice(staffId, deviceId) {
    const staff = await db.staff.findOne({ staff_id: staffId });
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // Prepare user data
    const userData = {
        user_id: staff.employee_id,
        name: `${staff.first_name} ${staff.last_name}`,
        privilege: 'User',
        password: staff.pin_code || '',
        card: staff.badge_number || ''
    };
    
    try {
        // Try to create user
        await pyzkApi.put('/user', userData, {
            params: {
                ip: device.ip_address,
                port: device.port
            }
        });
        
        // Update enrollment status
        await db.device_enrollments.update({
            device_id: deviceId,
            staff_id: staffId,
            enrollment_status: 'ENROLLED',
            enrolled_at: new Date()
        });
        
    } catch (error) {
        if (error.response?.status === 409) {
            // User exists, update instead
            await pyzkApi.post('/user', userData, {
                params: {
                    ip: device.ip_address,
                    port: device.port
                }
            });
        } else {
            throw error;
        }
    }
}
```

### 3. Biometric Template Management

#### Update Fingerprints
**PYZK Endpoint**: `POST /fingers?ip={ip}&port={port}`

**Integration Flow**:
```javascript
async function syncFingerprintsToDevice(staffId, deviceId) {
    // Get biometric templates from database
    const templates = await db.biometric_templates.findAll({
        staff_id: staffId,
        template_type: 'FINGERPRINT',
        is_active: true
    });
    
    const staff = await db.staff.findOne({ staff_id: staffId });
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // Format templates for PYZK API
    const templateData = {
        template: templates.map(t => ({
            size: t.template_data.length,
            valid: t.quality_score >= 50 ? 1 : 0,
            uid: parseInt(staff.employee_id),
            template: t.template_data,
            fid: t.finger_index
        }))
    };
    
    // Push to device
    await pyzkApi.post('/fingers', templateData, {
        params: {
            ip: device.ip_address,
            port: device.port
        }
    });
    
    // Log sync operation
    await db.sync_logs.create({
        device_id: deviceId,
        sync_type: 'USER',
        sync_direction: 'DB_TO_DEVICE',
        templates_synced: templates.length,
        sync_status: 'COMPLETED',
        started_at: new Date()
    });
}
```

### 4. Attendance Data Synchronization

#### Get Attendance Logs
**PYZK Endpoint**: `GET /attendances?ip={ip}&port={port}`

**Integration Flow**:
```javascript
async function pullAttendanceLogs(deviceId) {
    const device = await db.devices.findOne({ device_id: deviceId });
    const startTime = new Date();
    
    try {
        // Get attendance logs from device
        const response = await pyzkApi.get('/attendances', {
            params: {
                ip: device.ip_address,
                port: device.port
            }
        });
        
        let recordsProcessed = 0;
        let recordsFailed = 0;
        
        for (const log of response.data.attendances) {
            try {
                // Find staff by device user ID
                const enrollment = await db.device_enrollments.findOne({
                    device_id: deviceId,
                    device_user_id: log.user_id
                });
                
                if (enrollment) {
                    // Create punch log record
                    await db.punch_logs.create({
                        staff_id: enrollment.staff_id,
                        device_id: deviceId,
                        punch_time: new Date(log.timestamp),
                        punch_type: determinePunchType(log),
                        verification_mode: mapVerificationMode(log.status),
                        device_user_id: log.user_id,
                        device_log_id: log.id,
                        is_processed: false
                    });
                    
                    recordsProcessed++;
                }
            } catch (error) {
                recordsFailed++;
                console.error('Failed to process log:', error);
            }
        }
        
        // Update device last sync time
        await db.devices.update(deviceId, {
            last_sync_at: new Date()
        });
        
        // Create sync log
        await db.sync_logs.create({
            device_id: deviceId,
            sync_type: 'ATTENDANCE',
            sync_direction: 'DEVICE_TO_DB',
            records_synced: recordsProcessed,
            records_failed: recordsFailed,
            sync_status: recordsFailed > 0 ? 'PARTIAL' : 'COMPLETED',
            started_at: startTime,
            completed_at: new Date()
        });
        
        // Trigger attendance processing
        await processAttendanceRecords(deviceId);
        
    } catch (error) {
        await db.sync_logs.create({
            device_id: deviceId,
            sync_type: 'ATTENDANCE',
            sync_direction: 'DEVICE_TO_DB',
            sync_status: 'FAILED',
            error_message: error.message,
            started_at: startTime
        });
        throw error;
    }
}

function determinePunchType(log) {
    // Logic to determine if it's IN, OUT, BREAK_START, BREAK_END
    const hour = new Date(log.timestamp).getHours();
    if (hour < 10) return 'IN';
    if (hour > 17) return 'OUT';
    if (hour >= 12 && hour <= 14) {
        return log.punch % 2 === 0 ? 'BREAK_START' : 'BREAK_END';
    }
    return 'IN'; // Default
}

function mapVerificationMode(status) {
    const modeMap = {
        0: 'PASSWORD',
        1: 'FINGERPRINT',
        2: 'CARD',
        15: 'FACE'
    };
    return modeMap[status] || 'UNKNOWN';
}
```

### 5. Device Operations

#### Clear Attendance Logs
**PYZK Endpoint**: `DELETE /attendances?ip={ip}&port={port}`

**Integration Flow**:
```javascript
async function clearDeviceAttendanceLogs(deviceId) {
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // First, ensure all logs are synced
    await pullAttendanceLogs(deviceId);
    
    // Clear logs from device
    await pyzkApi.delete('/attendances', {
        params: {
            ip: device.ip_address,
            port: device.port
        }
    });
    
    // Log the operation
    await db.audit_logs.create({
        table_name: 'devices',
        record_id: deviceId,
        action: 'CLEAR_ATTENDANCE',
        performed_at: new Date()
    });
}
```

#### Device Backup and Restore

**Get Device Dump**: `GET /dump?ip={ip}&port={port}`
**Restore Device**: `PUT /dump?ip={ip}&port={port}`

```javascript
async function backupDevice(deviceId) {
    const device = await db.devices.findOne({ device_id: deviceId });
    
    // Get complete device dump
    const response = await pyzkApi.get('/dump', {
        params: {
            ip: device.ip_address,
            port: device.port
        }
    });
    
    // Store backup in database or file system
    const backup = {
        device_id: deviceId,
        backup_data: JSON.stringify(response.data),
        created_at: new Date()
    };
    
    // Save to S3 or local storage
    await saveBackup(backup);
    
    return backup;
}

async function restoreDevice(deviceId, backupId) {
    const device = await db.devices.findOne({ device_id: deviceId });
    const backup = await loadBackup(backupId);
    
    await pyzkApi.put('/dump', {
        data: JSON.parse(backup.backup_data)
    }, {
        params: {
            ip: device.ip_address,
            port: device.port
        }
    });
}
```

## Synchronization Service Architecture

```javascript
class DeviceSyncService {
    constructor() {
        this.syncQueue = new Queue('device-sync');
        this.syncInterval = 5 * 60 * 1000; // 5 minutes
    }
    
    async startSyncScheduler() {
        // Schedule periodic sync for all active devices
        setInterval(async () => {
            const devices = await db.devices.findAll({ 
                is_active: true 
            });
            
            for (const device of devices) {
                await this.syncQueue.add('sync-device', {
                    deviceId: device.device_id
                });
            }
        }, this.syncInterval);
    }
    
    async syncDevice(deviceId) {
        const syncId = uuid();
        
        try {
            // 1. Check device connectivity
            await this.checkDeviceHealth(deviceId);
            
            // 2. Sync attendance logs
            await this.syncAttendanceLogs(deviceId, syncId);
            
            // 3. Sync user changes (bidirectional)
            await this.syncUsers(deviceId, syncId);
            
            // 4. Update device status
            await this.updateDeviceStatus(deviceId, true);
            
        } catch (error) {
            await this.handleSyncError(deviceId, error, syncId);
        }
    }
    
    async checkDeviceHealth(deviceId) {
        const device = await db.devices.findOne({ device_id: deviceId });
        
        try {
            await pyzkApi.get('/device', {
                params: {
                    ip: device.ip_address,
                    port: device.port
                },
                timeout: 5000
            });
            
            await db.devices.update(deviceId, {
                is_online: true,
                last_heartbeat_at: new Date()
            });
            
        } catch (error) {
            await db.devices.update(deviceId, {
                is_online: false
            });
            throw new Error(`Device ${device.device_name} is offline`);
        }
    }
    
    async syncAttendanceLogs(deviceId, syncId) {
        // Implementation as shown above
    }
    
    async syncUsers(deviceId, syncId) {
        // Check for pending enrollments
        const pendingEnrollments = await db.device_enrollments.findAll({
            device_id: deviceId,
            enrollment_status: 'PENDING'
        });
        
        for (const enrollment of pendingEnrollments) {
            await this.pushUserToDevice(
                enrollment.staff_id, 
                deviceId
            );
        }
        
        // Pull users from device to check for manual additions
        await this.syncUsersFromDevice(deviceId);
    }
}
```

## API Gateway Layer

The main application API will wrap the PYZK API calls with business logic:

```javascript
// Main API Endpoints

class AttendanceAPIController {
    // Trigger manual sync for a device
    async POST('/api/devices/:deviceId/sync') {
        const { deviceId } = req.params;
        
        // Check permissions
        await checkPermission(req.user, 'devices.sync');
        
        // Queue sync job
        await syncQueue.add('sync-device', { deviceId });
        
        return {
            message: 'Sync initiated',
            deviceId
        };
    }
    
    // Enroll staff on devices
    async POST('/api/staff/:staffId/enroll') {
        const { staffId } = req.params;
        const { deviceIds } = req.body;
        
        // Check permissions
        await checkPermission(req.user, 'staff.update');
        
        // Create enrollment records
        for (const deviceId of deviceIds) {
            await db.device_enrollments.create({
                staff_id: staffId,
                device_id: deviceId,
                enrollment_status: 'PENDING'
            });
            
            // Queue enrollment job
            await enrollmentQueue.add('enroll-user', {
                staffId,
                deviceId
            });
        }
        
        return {
            message: 'Enrollment initiated',
            staffId,
            devices: deviceIds.length
        };
    }
    
    // Get real-time attendance status
    async GET('/api/attendance/realtime') {
        const { locationId, departmentId } = req.query;
        
        // Get latest punch logs
        const logs = await db.query(`
            SELECT 
                s.first_name,
                s.last_name,
                s.employee_id,
                pl.punch_time,
                pl.punch_type,
                d.device_name
            FROM punch_logs pl
            JOIN staff s ON pl.staff_id = s.staff_id
            JOIN devices d ON pl.device_id = d.device_id
            WHERE DATE(pl.punch_time) = CURRENT_DATE
            ${locationId ? 'AND s.location_id = :locationId' : ''}
            ${departmentId ? 'AND s.department_id = :departmentId' : ''}
            ORDER BY pl.punch_time DESC
            LIMIT 100
        `, { locationId, departmentId });
        
        return logs;
    }
}
```

## Error Handling and Recovery

```javascript
class DeviceSyncErrorHandler {
    async handleSyncError(deviceId, error, syncId) {
        const errorType = this.classifyError(error);
        
        switch (errorType) {
            case 'NETWORK_ERROR':
                await this.handleNetworkError(deviceId, error);
                break;
            
            case 'DEVICE_OFFLINE':
                await this.handleOfflineDevice(deviceId);
                break;
            
            case 'DATA_CONFLICT':
                await this.handleDataConflict(deviceId, error);
                break;
            
            default:
                await this.handleUnknownError(deviceId, error);
        }
        
        // Log error
        await db.sync_logs.create({
            sync_id: syncId,
            device_id: deviceId,
            sync_status: 'FAILED',
            error_message: error.message,
            completed_at: new Date()
        });
    }
    
    classifyError(error) {
        if (error.code === 'ECONNREFUSED' || error.code === 'ETIMEDOUT') {
            return 'NETWORK_ERROR';
        }
        if (error.message.includes('offline')) {
            return 'DEVICE_OFFLINE';
        }
        if (error.message.includes('conflict') || error.message.includes('duplicate')) {
            return 'DATA_CONFLICT';
        }
        return 'UNKNOWN';
    }
    
    async handleNetworkError(deviceId, error) {
        // Implement exponential backoff retry
        const retryCount = await this.getRetryCount(deviceId);
        
        if (retryCount < 3) {
            const delay = Math.pow(2, retryCount) * 1000;
            
            setTimeout(async () => {
                await syncQueue.add('sync-device', {
                    deviceId,
                    retryCount: retryCount + 1
                });
            }, delay);
        } else {
            // Alert administrators
            await this.alertAdministrators(deviceId, error);
        }
    }
}
```

## Security Considerations

### API Key Management
```javascript
class PyzkAPIClient {
    constructor() {
        this.apiKey = process.env.PYZK_API_KEY;
        this.baseURL = process.env.PYZK_API_URL || 'http://pyzk-service:8001';
    }
    
    async request(method, endpoint, options = {}) {
        const config = {
            method,
            url: `${this.baseURL}${endpoint}`,
            headers: {
                'X-Pretty-Key': this.apiKey,
                ...options.headers
            },
            ...options
        };
        
        try {
            const response = await axios(config);
            return response.data;
        } catch (error) {
            this.logAPIError(error);
            throw error;
        }
    }
}
```

### Device Communication Encryption
- Use VPN or secure network for device communication
- Implement TLS if devices support it
- Rotate API keys regularly
- Audit all device operations

## Performance Optimization

### Batch Processing
```javascript
async function batchSyncAttendance() {
    const BATCH_SIZE = 1000;
    
    // Get unprocessed logs in batches
    let offset = 0;
    let hasMore = true;
    
    while (hasMore) {
        const logs = await db.punch_logs.findAll({
            is_processed: false,
            limit: BATCH_SIZE,
            offset
        });
        
        if (logs.length < BATCH_SIZE) {
            hasMore = false;
        }
        
        await processLogBatch(logs);
        offset += BATCH_SIZE;
    }
}
```


## Monitoring and Metrics

```javascript
class SyncMetrics {
    async recordSyncMetric(deviceId, metric, value) {
        await prometheus.gauge(
            `device_sync_${metric}`,
            value,
            { device_id: deviceId }
        );
    }
    
    async getDeviceHealthMetrics() {
        const devices = await db.query(`
            SELECT 
                d.device_id,
                d.device_name,
                d.is_online,
                d.last_sync_at,
                COUNT(de.enrollment_id) as enrolled_users,
                COUNT(pl.log_id) as logs_today
            FROM devices d
            LEFT JOIN device_enrollments de ON d.device_id = de.device_id
            LEFT JOIN punch_logs pl ON d.device_id = pl.device_id 
                AND DATE(pl.punch_time) = CURRENT_DATE
            GROUP BY d.device_id
        `);
        
        return devices;
    }
}
```

## Testing Strategy

```javascript
// Mock PYZK API for testing
class MockPyzkAPI {
    constructor() {
        this.devices = new Map();
        this.users = new Map();
        this.attendances = [];
    }
    
    async getDevice(ip, port) {
        return {
            firmware_version: '1.0.0',
            user_capacity: 1000,
            log_capacity: 100000,
            device_name: `Mock Device ${ip}`
        };
    }
    
    async getUsers(ip, port, includeFingers) {
        return {
            users: Array.from(this.users.values())
        };
    }
    
    async createUser(ip, port, userData) {
        this.users.set(userData.user_id, userData);
        return { success: true };
    }
}

// Integration tests
describe('Device Sync Integration', () => {
    let syncService;
    let mockPyzkAPI;
    
    beforeEach(() => {
        mockPyzkAPI = new MockPyzkAPI();
        syncService = new DeviceSyncService(mockPyzkAPI);
    });
    
    test('Should sync attendance logs from device', async () => {
        // Add test attendance logs
        mockPyzkAPI.attendances = [
            {
                user_id: '123',
                timestamp: '2024-01-01T09:00:00',
                status: 1
            }
        ];
        
        await syncService.syncAttendanceLogs('device-1');
        
        const logs = await db.punch_logs.findAll({
            device_id: 'device-1'
        });
        
        expect(logs).toHaveLength(1);
        expect(logs[0].device_user_id).toBe('123');
    });
});
```