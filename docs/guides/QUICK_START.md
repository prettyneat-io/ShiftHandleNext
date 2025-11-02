# ZK Simulator Quick Start Guide

## Basic Usage

### Start the Simulator
```bash
cd PunchClockApi/Device
python3 zk_simulator.py
```

Default settings:
- IP: 0.0.0.0 (all interfaces)
- Port: 4370
- Protocol: TCP
- Password: None
- Database: `zk_simulator.db`

### Start with Custom Settings
```bash
# Specific IP and port
python3 zk_simulator.py --ip 192.168.1.100 --port 4370

# With password protection
python3 zk_simulator.py --password 12345

# UDP mode
python3 zk_simulator.py --udp

# Custom database file
python3 zk_simulator.py --db my_device.db
```

## Testing the Simulator

### Run Unit Tests
```bash
python3 test_simulator_db.py
```
Tests database operations (create, read, update, delete).

### Run Integration Tests
```bash
# Requires pyzk library: pip install pyzk
python3 test_simulator_integration.py
```
Tests full simulator with pyzk client, including data persistence.

## Working with the Database

### View Database Contents
```bash
# All users
sqlite3 zk_simulator.db "SELECT uid, name, privilege FROM users;"

# All templates
sqlite3 zk_simulator.db "SELECT uid, fid, length(template_data) as size FROM templates;"

# Attendance records
sqlite3 zk_simulator.db "SELECT * FROM attendance_records LIMIT 10;"

# Statistics
sqlite3 zk_simulator.db "
  SELECT 
    (SELECT COUNT(*) FROM users) as users,
    (SELECT COUNT(*) FROM templates) as templates,
    (SELECT COUNT(*) FROM attendance_records) as records;
"
```

### Reset Database
```bash
# Delete the database file
rm zk_simulator.db

# Restart simulator (will create fresh database with default users)
python3 zk_simulator.py
```

### Backup Database
```bash
# While simulator is stopped
cp zk_simulator.db zk_simulator_backup_$(date +%Y%m%d).db

# Or use SQLite backup
sqlite3 zk_simulator.db ".backup zk_simulator_backup.db"
```

## Default Users

The simulator seeds with 3 default users on first run:

| UID | Name     | Privilege | Password | Card   |
|-----|----------|-----------|----------|--------|
| 1   | Admin    | 14 (admin)| -        | -      |
| 2   | User001  | 0 (user)  | 12345    | 123456 |
| 3   | User002  | 0 (user)  | -        | 234567 |

## Using with pyzk Client

```python
from zk import ZK

# Connect to simulator
zk = ZK('127.0.0.1', port=4370, timeout=5)
conn = zk.connect()

# Get device info
print("Firmware:", conn.get_firmware_version())
print("Serial:", conn.get_serialnumber())

# Get users
users = conn.get_users()
for user in users:
    print(f"User {user.uid}: {user.name}")

# Add user
conn.set_user(uid=10, name='NewUser', privilege=0, password='', group_id='', user_id='10')

# Get templates
templates = conn.get_templates()
print(f"Templates: {len(templates)}")

# Get attendance records
attendance = conn.get_attendance()
for att in attendance:
    print(f"UID {att.uid}: {att.timestamp}")

# Disconnect
conn.disconnect()
```

## Common Tasks

### Add Multiple Users
```python
from zk import ZK

zk = ZK('127.0.0.1', port=4370)
conn = zk.connect()

# Add 10 test users
for i in range(10, 20):
    conn.set_user(
        uid=i, 
        name=f'TestUser{i:03d}', 
        privilege=0, 
        password='', 
        group_id='', 
        user_id=str(i)
    )
    print(f"Added user {i}")

conn.disconnect()
```

### Simulate Multiple Devices
```bash
# Terminal 1 - Main entrance
python3 zk_simulator.py --port 4370 --db entrance.db &

# Terminal 2 - Back door
python3 zk_simulator.py --port 4371 --db backdoor.db &

# Terminal 3 - Office
python3 zk_simulator.py --port 4372 --db office.db &

# Connect to different devices
# Device 1: 127.0.0.1:4370
# Device 2: 127.0.0.1:4371
# Device 3: 127.0.0.1:4372
```

## Troubleshooting

### Port Already in Use
```bash
# Check what's using the port
sudo lsof -i :4370

# Or use a different port
python3 zk_simulator.py --port 4371
```

### Database Locked
```bash
# Make sure only one simulator is using the database
# Close any SQLite connections
pkill -f "sqlite3 zk_simulator.db"
```

### Connection Timeout
```python
# Increase timeout in pyzk client
zk = ZK('127.0.0.1', port=4370, timeout=10)
```

### Data Not Persisting
```bash
# Check database file permissions
ls -l zk_simulator.db

# Ensure database is in writable directory
# Try using absolute path
python3 zk_simulator.py --db /tmp/zk_simulator.db
```

## Advanced Usage

### Custom Database Location
```bash
# Use project-specific database
python3 zk_simulator.py --db ~/projects/myapp/device_data.db

# Use temporary database (lost on reboot)
python3 zk_simulator.py --db /tmp/test_device.db
```

### Running as Background Service
```bash
# Start in background
nohup python3 zk_simulator.py > simulator.log 2>&1 &

# Save process ID
echo $! > simulator.pid

# Stop the simulator
kill $(cat simulator.pid)

# View logs
tail -f simulator.log
```

### Docker Container (Future Enhancement)
```dockerfile
FROM python:3.9-slim
WORKDIR /app
COPY zk_simulator.py .
COPY zk/ ./zk/
EXPOSE 4370
CMD ["python3", "zk_simulator.py", "--ip", "0.0.0.0"]
```

## Performance Tips

1. **Use Local Filesystem**: Keep database on local disk for best performance
2. **Regular Cleanup**: Archive old attendance records periodically
3. **Index Management**: Database is already optimized with indexes
4. **Connection Pooling**: Each operation uses a new connection (optimal for simulator workload)

## Getting Help

- **Documentation**: See `ZK_SIMULATOR_README.md` for detailed information
- **Improvements**: See `SIMULATOR_IMPROVEMENTS.md` for technical details
- **Issues**: Check GitHub issues or create a new one
- **Protocol**: Refer to ZKTeco protocol documentation for advanced features
