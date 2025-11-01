# ZKTeco Device Simulator

A Python-based simulator that emulates a ZKTeco attendance device, allowing you to test the pyzk library without needing physical hardware.

## Features

The simulator implements the core ZKTeco protocol and supports:

- **Connection Management**: TCP and UDP connections with optional password authentication
- **Device Information**: Firmware version, serial number, platform, device name, MAC address
- **Time Management**: Get current device time
- **Network Information**: IP address, netmask, gateway
- **User Management**: Retrieve user list with details (UID, name, privilege, card number)
- **Capacity Information**: User, fingerprint, and attendance record capacity
- **Device Control**: Enable/disable device commands

## Requirements

- Python 3.6+
- pyzk library (for testing)

## Usage

### Starting the Simulator

Run the simulator with default settings (TCP on 0.0.0.0:4370):

```bash
python3 zk_simulator.py
```

### Command-line Options

```bash
python3 zk_simulator.py --help
```

Options:
- `--ip IP`: IP address to bind (default: 0.0.0.0)
- `--port PORT`: Port to bind (default: 4370)
- `--password PASSWORD`: Device password (default: 0 - no password)
- `--udp`: Use UDP instead of TCP

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
- **Users**: 3 pre-configured users (Admin, User001, User002)
- **Capacity**: 3000 users, 10000 fingerprints, 100000 records

## Supported Commands

The simulator currently supports these ZKTeco protocol commands:

- `CMD_CONNECT (1000)`: Connection request
- `CMD_EXIT (1001)`: Disconnect request
- `CMD_AUTH (1102)`: Authentication
- `CMD_ENABLEDEVICE (1002)`: Enable device
- `CMD_DISABLEDEVICE (1003)`: Disable device
- `CMD_GET_VERSION (1100)`: Get firmware version
- `CMD_GET_TIME (201)`: Get device time
- `CMD_OPTIONS_RRQ (11)`: Read device options/parameters
- `CMD_GET_FREE_SIZES (50)`: Get capacity information
- `CMD_USERTEMP_RRQ (9)`: Get users list
- `CMD_FREE_DATA (1502)`: Free data buffer
- `CMD_REG_EVENT (500)`: Register for events
- `CMD_STARTVERIFY (60)`: Start verification mode

## Extending the Simulator

You can modify the simulator to add more features:

1. **Add More Users**: Edit the `self.users` list in the `__init__` method
2. **Add Attendance Records**: Implement `CMD_ATTLOG_RRQ` handler
3. **Add Fingerprint Templates**: Implement template storage and retrieval
4. **Add Real-time Events**: Implement event broadcasting for live capture

## Limitations

The simulator is designed for testing and development. It currently:

- Does not store persistent data (users, records reset on restart)
- Does not implement all protocol commands (attendance logs, templates, etc.)
- Does not simulate fingerprint scanning or real-time events
- Simplified authentication (accepts any password if password is set)

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

## License

This simulator is part of the pyzk project and follows the same license.
