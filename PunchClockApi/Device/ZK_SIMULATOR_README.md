# ZKTeco Device Simulator

A Python-based simulator that emulates a ZKTeco attendance device, allowing you to test the pyzk library without needing physical hardware. The simulator uses a local SQLite database for persistent data storage.

## Features

The simulator implements the core ZKTeco protocol and supports:

- **Connection Management**: TCP and UDP connections with optional password authentication
- **Device Information**: Firmware version, serial number, platform, device name, MAC address
- **Time Management**: Get current device time
- **Network Information**: IP address, netmask, gateway
- **User Management**: Create, read, update, and delete users with persistent storage
- **Fingerprint Templates**: Store and manage fingerprint enrollment with persistent storage
- **Attendance Records**: Track attendance logs with persistent storage
- **Capacity Information**: User, fingerprint, and attendance record capacity
- **Device Control**: Enable/disable device commands
- **Persistent Storage**: SQLite database for all device data (users, templates, attendance)

## Requirements

- Python 3.6+
- pyzk library (for testing)

## Usage

### Starting the Simulator

Run the simulator with default settings (TCP on 0.0.0.0:4370):

```bash
python3 zk_simulator.py
```

The simulator will automatically:
- Create a SQLite database file (`zk_simulator.db`) if it doesn't exist
- Initialize the database schema with required tables
- Seed with 3 default users if the database is empty
- Load existing data on subsequent runs

### Command-line Options

```bash
python3 zk_simulator.py --help
```

Options:
- `--ip IP`: IP address to bind (default: 0.0.0.0)
- `--port PORT`: Port to bind (default: 4370)
- `--password PASSWORD`: Device password (default: 0 - no password)
- `--udp`: Use UDP instead of TCP
- `--db DATABASE`: SQLite database path (default: zk_simulator.db)

### Examples

Run on a specific IP and port:
```bash
python3 zk_simulator.py --ip 192.168.1.100 --port 4370
```

Run with password protection:
```bash
python3 zk_simulator.py --password 12345
```

Run in UDP mode:
```bash
python3 zk_simulator.py --udp
```

Use a custom database file:
```bash
python3 zk_simulator.py --db my_device.db
```

### Database Structure

The simulator uses SQLite with the following tables:

#### users
- `uid` (INTEGER PRIMARY KEY) - Unique user ID
- `privilege` (INTEGER) - User privilege level (0=user, 14=admin)
- `password` (BLOB) - User password
- `name` (BLOB) - User name
- `card` (INTEGER) - Card number
- `group_id` (TEXT) - Group ID
- `user_id` (TEXT) - User ID string
- `created_at` (TIMESTAMP) - Creation timestamp

#### templates
- `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
- `uid` (INTEGER) - User ID
- `fid` (INTEGER) - Finger ID (0-9)
- `valid` (INTEGER) - Template validity flag
- `template_data` (BLOB) - Fingerprint template data
- `created_at` (TIMESTAMP)
- UNIQUE constraint on (uid, fid)

#### attendance_records
- `id` (INTEGER PRIMARY KEY AUTOINCREMENT)
- `uid` (INTEGER) - User ID
- `user_id` (TEXT) - User ID string
- `timestamp_encoded` (BLOB) - Encoded timestamp
- `status` (INTEGER) - Attendance status
- `punch` (INTEGER) - Punch type
- `recorded_at` (TIMESTAMP)

### Managing the Database

**View database contents:**
```bash
sqlite3 zk_simulator.db "SELECT * FROM users;"
sqlite3 zk_simulator.db "SELECT uid, fid, length(template_data) as template_size FROM templates;"
sqlite3 zk_simulator.db "SELECT COUNT(*) as total_records FROM attendance_records;"
```

**Reset the database:**
```bash
rm zk_simulator.db
# Restart the simulator to create a fresh database with default users
python3 zk_simulator.py
```

**Use multiple device simulations:**
```bash
# Device 1
python3 zk_simulator.py --port 4370 --db device1.db

# Device 2 (in another terminal)
python3 zk_simulator.py --port 4371 --db device2.db
```

### Testing the Simulator

Once the simulator is running, open another terminal and run the test script:

```bash
python3 test_simulator.py
```

Or test with a specific IP/port:
```bash
python3 test_simulator.py --ip 127.0.0.1 --port 4370
```

### Using with pyzk Library

You can use the simulator with the pyzk library just like a real device:

```python
from zk import ZK

# Connect to simulator
zk = ZK('127.0.0.1', port=4370)
conn = zk.connect()

# Get device info
print("Firmware:", conn.get_firmware_version())
print("Serial:", conn.get_serialnumber())

# Get users
users = conn.get_users()
for user in users:
    print(f"User {user.uid}: {user.name}")

# Disconnect
conn.disconnect()
```

## Simulated Device Data

The simulator comes pre-configured with:

- **Firmware Version**: Ver 6.60 Nov 13 2019
- **Serial Number**: DGD9190019050335743
- **Platform**: ZEM560
- **Device Name**: ZKTeco Device
- **MAC Address**: 00:17:61:C8:EC:17
- **Default Users** (seeded on first run): 
  - Admin (uid=1, privilege=14)
  - User001 (uid=2, password=12345, card=123456)
  - User002 (uid=3, card=234567)
- **Capacity**: 3000 users, 10000 fingerprints, 100000 attendance records

## Data Persistence

All user data, fingerprint templates, and attendance records are stored in the SQLite database and persist across simulator restarts. This means:

- Users enrolled in one session remain available in future sessions
- Fingerprint templates are preserved and can be verified
- Attendance records accumulate over time
- You can stop and restart the simulator without losing data

## Supported Commands

The simulator currently supports these ZKTeco protocol commands:

- `CMD_CONNECT (1000)`: Connection request
- `CMD_EXIT (1001)`: Disconnect request
- `CMD_AUTH (1102)`: Authentication
- `CMD_ENABLEDEVICE (1002)`: Enable device
- `CMD_DISABLEDEVICE (1003)`: Disable device
- `CMD_GET_VERSION (1100)`: Get firmware version
- `CMD_GET_TIME (201)`: Get device time
- `CMD_SET_TIME (202)`: Set device time
- `CMD_OPTIONS_RRQ (11)`: Read device options/parameters
- `CMD_OPTIONS_WRQ (12)`: Write device options/parameters
- `CMD_GET_FREE_SIZES (50)`: Get capacity information
- `CMD_USERTEMP_RRQ (9)`: Get users list
- `CMD_USER_WRQ (8)`: Add/update user
- `CMD_DELETE_USER (18)`: Delete user
- `CMD_DELETE_USERTEMP (19)`: Delete fingerprint template
- `CMD_DB_RRQ (7)`: Read fingerprint templates
- `CMD_ATTLOG_RRQ (13)`: Read attendance logs
- `CMD_STARTENROLL (61)`: Start fingerprint enrollment
- `CMD_CANCELCAPTURE (62)`: Cancel enrollment
- `CMD_PREPARE_BUFFER (1503)`: Prepare data buffer
- `CMD_PREPARE_DATA (1500)`: Initialize upload buffer
- `CMD_DATA (1501)`: Data transfer
- `CMD_SAVE_USERTEMPS (110)`: Save user templates (batch upload)
- `CMD_FREE_DATA (1502)`: Free data buffer
- `CMD_REFRESHDATA (1013)`: Refresh device data
- `CMD_REG_EVENT (500)`: Register for events
- `CMD_STARTVERIFY (60)`: Start verification mode
- `CMD_GET_PINWIDTH (69)`: Get PIN width
- `CMD_UNLOCK (31)`: Unlock door
- `CMD_TESTVOICE (1017)`: Test voice output

## Extending the Simulator

You can modify the simulator to add more features:

1. **Add More Default Users**: Edit the seeding code in `_init_database()` method
2. **Add Attendance Records**: Use `_insert_attendance_record()` to add punch logs
3. **Custom Device Info**: Modify device properties in `__init__()` (firmware, serial, etc.)
4. **Implement Additional Commands**: Add new handler methods following existing patterns

## Limitations

The simulator is designed for testing and development. Current limitations:

- Does not simulate actual fingerprint scanning hardware (uses dummy templates)
- Simplified enrollment simulation (automatic 3-scan process)
- Authentication accepts any password if password protection is enabled
- Some advanced protocol features may not be fully implemented

## Troubleshooting

**Connection refused**: Make sure the simulator is running and the IP/port are correct.

**Timeout errors**: Increase the timeout when creating the ZK object:
```python
zk = ZK('127.0.0.1', port=4370, timeout=10)
```

**Authentication errors**: If using password, make sure to pass it to the ZK constructor:
```python
zk = ZK('127.0.0.1', port=4370, password=12345)
```

**Database locked**: Close other connections to the database file before starting the simulator.

**Data not persisting**: Check that the database file has write permissions and is in a writable directory.

## License

This simulator is part of the pyzk project and follows the same license.
