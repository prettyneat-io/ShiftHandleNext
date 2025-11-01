#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
ZKTeco Device Simulator

This script simulates a ZKTeco attendance device that responds to pyzk client requests.
It implements the basic protocol to handle connections, authentication, and device info queries.
"""

import socket
import struct
import sys
from datetime import datetime
import codecs
import time
import threading

# Constants from ZKTeco protocol
USHRT_MAX = 65535

# Commands
CMD_CONNECT = 1000
CMD_EXIT = 1001
CMD_ENABLEDEVICE = 1002
CMD_DISABLEDEVICE = 1003
CMD_RESTART = 1004
CMD_POWEROFF = 1005
CMD_GET_TIME = 201
CMD_SET_TIME = 202
CMD_GET_VERSION = 1100
CMD_AUTH = 1102
CMD_OPTIONS_RRQ = 11
CMD_OPTIONS_WRQ = 12
CMD_GET_FREE_SIZES = 50
CMD_USERTEMP_RRQ = 9
CMD_ATTLOG_RRQ = 13
CMD_FREE_DATA = 1502
CMD_PREPARE_BUFFER = 1503
CMD_PREPARE_DATA = 1500
CMD_DATA = 1501
CMD_REG_EVENT = 500
CMD_STARTVERIFY = 60
CMD_GET_PINWIDTH = 69
CMD_UNLOCK = 31
CMD_TESTVOICE = 1017
CMD_USER_WRQ = 8
CMD_DELETE_USER = 18
CMD_REFRESHDATA = 1013
CMD_DELETE_USERTEMP = 19
CMD_STARTENROLL = 61
CMD_CANCELCAPTURE = 62
_CMD_GET_USERTEMP = 88
CMD_DB_RRQ = 7
_CMD_SAVE_USERTEMPS = 110
_CMD_PREPARE_BUFFER = 1503
_CMD_READ_BUFFER = 1504

# Responses
CMD_ACK_OK = 2000
CMD_ACK_ERROR = 2001
CMD_ACK_UNAUTH = 2005
CMD_PREPARE_DATA = 1500
CMD_DATA = 1501

# TCP header constants
USHRT_MAX = 65535

MACHINE_PREPARE_DATA_1 = 20560  # 0x5050
MACHINE_PREPARE_DATA_2 = 32130  # 0x7d82

# Function types
FCT_USER = 5
FCT_ATTLOG = 1
FCT_FINGERTMP = 2


class ZKSimulator:
    """Simulates a ZKTeco attendance device"""
    
    def __init__(self, ip='0.0.0.0', port=4370, password=0, use_tcp=True):
        self.ip = ip
        self.port = port
        self.password = password
        self.use_tcp = use_tcp
        self.session_id = 0
        self.is_authenticated = False
        
        # Simulated device data
        self.firmware_version = "Ver 6.60 Nov 13 2019"
        self.serial_number = "DGD9190019050335743"
        self.platform = "ZEM560"
        self.device_name = "ZKTeco Device"
        self.mac_address = "00:17:61:C8:EC:17"
        
        # User data - simulate some basic users
        self.users = [
            # uid, privilege, password, name, card, group_id, user_id
            (1, 0, b'', b'Admin', 0, 0, 1),
            (2, 0, b'12345', b'User001', 123456, 0, 2),
            (3, 0, b'', b'User002', 234567, 0, 3),
        ]
        
        # Fingerprint templates - (uid, fid, valid, template_data)
        self.templates = []
        
        # Attendance records - (user_id, timestamp_encoded, status, punch, uid)
        self.attendance_records = []
        
        # Event registration
        self.registered_events = 0
        self.enrollment_mode = False
        self.enrollment_uid = 0
        self.enrollment_fid = 0
        self.enrollment_conn = None
        
        # Buffer for batch operations
        self.data_buffer = b''
        
        # Device capacity
        self.users_count = len(self.users)
        self.users_capacity = 3000
        self.fingers_count = 5
        self.fingers_capacity = 10000
        self.records_count = 150
        self.records_capacity = 100000
        
    def create_checksum(self, packet):
        """Calculate checksum for packet"""
        checksum = 0
        packet_bytes = list(packet)
        length = len(packet_bytes)
        
        while length > 1:
            checksum += struct.unpack('H', bytes([packet_bytes[0], packet_bytes[1]]))[0]
            packet_bytes = packet_bytes[2:]
            if checksum > USHRT_MAX:
                checksum -= USHRT_MAX
            length -= 2
            
        if length:
            checksum += packet_bytes[-1]
            
        while checksum > USHRT_MAX:
            checksum -= USHRT_MAX
            
        checksum = ~checksum
        
        while checksum < 0:
            checksum += USHRT_MAX
            
        return struct.pack('H', checksum)
    
    def create_header(self, command, session_id, reply_id, data=b''):
        """Create packet header"""
        buf = struct.pack('<4H', command, 0, session_id, reply_id) + data
        buf_list = struct.unpack('8B' + '%sB' % len(data), buf)
        checksum = struct.unpack('H', self.create_checksum(buf_list))[0]
        header = struct.pack('<4H', command, checksum, session_id, reply_id)
        return header + data
    
    def create_tcp_top(self, packet):
        """Create TCP top header"""
        length = len(packet)
        top = struct.pack('<HHI', MACHINE_PREPARE_DATA_1, MACHINE_PREPARE_DATA_2, length)
        return top + packet
    
    def encode_time(self, dt):
        """Encode datetime to device format"""
        d = (
            ((dt.year % 100) * 12 * 31 + ((dt.month - 1) * 31) + dt.day - 1) *
            (24 * 60 * 60) + (dt.hour * 60 + dt.minute) * 60 + dt.second
        )
        return struct.pack('I', d)
    
    def decode_time(self, t):
        """Decode a timestamp from the device"""
        second = t % 60
        t = t // 60
        minute = t % 60
        t = t // 60
        hour = t % 24
        t = t // 24
        day = t % 31 + 1
        t = t // 31
        month = t % 12 + 1
        t = t // 12
        year = t + 2000
        return datetime(year, month, day, hour, minute, second)
    
    def handle_connect(self, session_id, reply_id):
        """Handle connection request"""
        print("  -> Handling CMD_CONNECT")
        self.session_id = session_id if session_id != 0 else 1000
        
        if self.password == 0:
            # No password required
            self.is_authenticated = True
            response = self.create_header(CMD_ACK_OK, self.session_id, reply_id)
        else:
            # Password required - send UNAUTH
            response = self.create_header(CMD_ACK_UNAUTH, self.session_id, reply_id)
        
        return response
    
    def handle_auth(self, session_id, reply_id, data):
        """Handle authentication"""
        print("  -> Handling CMD_AUTH")
        # For simplicity, accept any auth attempt
        self.is_authenticated = True
        self.session_id = session_id
        response = self.create_header(CMD_ACK_OK, self.session_id, reply_id)
        return response
    
    def handle_exit(self, session_id, reply_id):
        """Handle disconnect request"""
        print("  -> Handling CMD_EXIT")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        self.is_authenticated = False
        return response
    
    def handle_enable_device(self, session_id, reply_id):
        """Handle enable device"""
        print("  -> Handling CMD_ENABLEDEVICE")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_disable_device(self, session_id, reply_id):
        """Handle disable device"""
        print("  -> Handling CMD_DISABLEDEVICE")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_get_version(self, session_id, reply_id):
        """Handle get firmware version"""
        print("  -> Handling CMD_GET_VERSION")
        data = self.firmware_version.encode() + b'\x00'
        response = self.create_header(CMD_ACK_OK, session_id, reply_id, data)
        return response
    
    def handle_get_time(self, session_id, reply_id):
        """Handle get time"""
        print("  -> Handling CMD_GET_TIME")
        now = datetime.now()
        time_data = self.encode_time(now)
        response = self.create_header(CMD_ACK_OK, session_id, reply_id, time_data)
        return response
    
    def handle_set_time(self, session_id, reply_id, data):
        """Handle set time"""
        print("  -> Handling CMD_SET_TIME")
        if len(data) >= 4:
            timestamp = struct.unpack('I', data[:4])[0]
            # Decode the timestamp to verify it
            dt = self.decode_time(timestamp)
            print(f"     Setting time to: {dt}")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_options_rrq(self, session_id, reply_id, data):
        """Handle options read request"""
        print(f"  -> Raw data: {data.hex() if data else 'empty'}")
        option_name = data.rstrip(b'\x00').decode('utf-8', errors='ignore')
        print(f"  -> Handling CMD_OPTIONS_RRQ: {option_name}")
        
        response_data = b''
        
        if option_name == '~SerialNumber':
            response_data = f"~SerialNumber={self.serial_number}\x00".encode()
        elif option_name == '~Platform':
            response_data = f"~Platform={self.platform}\x00".encode()
        elif option_name == 'MAC':
            response_data = f"MAC={self.mac_address}\x00".encode()
        elif option_name == '~DeviceName':
            response_data = f"~DeviceName={self.device_name}\x00".encode()
        elif option_name == 'ZKFaceVersion':
            response_data = b"ZKFaceVersion=0\x00"
        elif option_name == '~ZKFPVersion':
            response_data = b"~ZKFPVersion=10\x00"
        elif option_name == 'IPAddress':
            response_data = f"IPAddress={self.ip}\x00".encode()
        elif option_name == 'NetMask':
            response_data = b"NetMask=255.255.255.0\x00"
        elif option_name == 'GATEIPAddress':
            response_data = b"GATEIPAddress=192.168.1.1\x00"
        elif option_name == '~ExtendFmt':
            response_data = b"~ExtendFmt=0\x00"
        elif option_name == '~UserExtFmt':
            response_data = b"~UserExtFmt=0\x00"
        elif option_name == 'FaceFunOn':
            response_data = b"FaceFunOn=0\x00"
        elif option_name == 'CompatOldFirmware':
            response_data = b"CompatOldFirmware=0\x00"
        else:
            response_data = b""
        
        print(f"  -> Response data: {response_data.hex() if response_data else 'empty'} ({len(response_data)} bytes)")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id, response_data)
        print(f"  -> Full response: {len(response)} bytes")
        return response
    
    def handle_get_free_sizes(self, session_id, reply_id):
        """Handle get free sizes"""
        print("  -> Handling CMD_GET_FREE_SIZES")
        
        # Build size data (20 integers = 80 bytes)
        size_data = struct.pack('20i',
            0,  # 0
            0,  # 1
            0,  # 2
            0,  # 3
            self.users_count,  # 4 - users
            0,  # 5
            self.fingers_count,  # 6 - fingers
            0,  # 7
            self.records_count,  # 8 - records
            0,  # 9
            0,  # 10 - dummy
            0,  # 11
            0,  # 12 - cards
            0,  # 13
            self.fingers_capacity,  # 14 - fingers capacity
            self.users_capacity,  # 15 - users capacity
            self.records_capacity,  # 16 - records capacity
            self.fingers_capacity - self.fingers_count,  # 17 - fingers available
            self.users_capacity - self.users_count,  # 18 - users available
            self.records_capacity - self.records_count,  # 19 - records available
        )
        
        # Add face data (3 integers = 12 bytes)
        face_data = struct.pack('3i', 0, 0, 0)
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id, size_data + face_data)
        return response
    
    def handle_usertemp_rrq(self, session_id, reply_id):
        """Handle user template read request - return users"""
        print("  -> Handling CMD_USERTEMP_RRQ (get users)")
        
        # Pack user data (using 72-byte format for modern devices)
        user_data = b''
        for uid, privilege, password, name, card, group_id, user_id in self.users:
            # Format: <HB8s24sIx7sx24s (72 bytes)
            password_bytes = str(password).encode()[:8].ljust(8, b'\x00') if isinstance(password, (int, str)) else password.ljust(8, b'\x00')[:8]
            name_bytes = name if isinstance(name, bytes) else name.encode()
            name_bytes = name_bytes[:24].ljust(24, b'\x00')
            user_id_str = str(user_id).encode()[:24].ljust(24, b'\x00')
            group_id_str = str(group_id).encode()[:7].ljust(7, b'\x00')
            
            user_entry = struct.pack('<HB8s24sIx7sx24s',
                uid,
                privilege,
                password_bytes,
                name_bytes,
                card,
                group_id_str,
                user_id_str
            )
            user_data += user_entry
        
        # Total size at the beginning
        total_size = len(user_data)
        full_data = struct.pack('I', total_size) + user_data
        
        # Return as PREPARE_DATA + DATA
        response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
        return response
    
    def handle_free_data(self, session_id, reply_id):
        """Handle free data buffer"""
        print("  -> Handling CMD_FREE_DATA")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_prepare_buffer(self, session_id, reply_id, data):
        """Handle prepare buffer command"""
        print("  -> Handling CMD_PREPARE_BUFFER")
        print(f"     Data length: {len(data)} bytes")
        
        # Parse command (b=1, h=2, i=4, i=4 = 11 bytes total)
        if len(data) >= 11:
            flag, command, fct, ext = struct.unpack('<bhii', data[:11])
            print(f"     Buffer command: flag={flag}, cmd={command}, fct={fct}, ext={ext}")
            
            # Check what type of data is requested
            
            if fct == FCT_USER:
                # Return user data
                print("     Returning user data")
                user_data = b''
                for uid, privilege, password, name, card, group_id, user_id in self.users:
                    # Format: <HB8s24sIx7sx24s (72 bytes)
                    password_bytes = str(password).encode()[:8].ljust(8, b'\x00') if isinstance(password, (int, str)) else password.ljust(8, b'\x00')[:8]
                    name_bytes = name if isinstance(name, bytes) else name.encode()
                    name_bytes = name_bytes[:24].ljust(24, b'\x00')
                    user_id_str = str(user_id).encode()[:24].ljust(24, b'\x00')
                    group_id_str = str(group_id).encode()[:7].ljust(7, b'\x00')
                    
                    user_entry = struct.pack('<HB8s24sIx7sx24s',
                        uid,
                        privilege,
                        password_bytes,
                        name_bytes,
                        card,
                        group_id_str,
                        user_id_str
                    )
                    user_data += user_entry
                
                # Total size at the beginning
                total_size = len(user_data)
                full_data = struct.pack('I', total_size) + user_data
                response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
            elif fct == FCT_FINGERTMP:
                # Return fingerprint templates
                print("     Returning fingerprint templates")
                template_data = b''
                for uid, fid, valid, tmpl in self.templates:
                    size = len(tmpl) + 6
                    template_data += struct.pack('<HHbb', size, uid, fid, valid) + tmpl
                
                total_size = len(template_data)
                full_data = struct.pack('I', total_size) + template_data
                response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
            elif fct == FCT_ATTLOG:
                # Return attendance records
                print("     Returning attendance records")
                att_data = b''
                for user_id, timestamp, status, punch, uid in self.attendance_records:
                    # 40-byte format
                    user_id_bytes = str(user_id).encode()[:24].ljust(24, b'\x00')
                    att_data += struct.pack('<H24sB4sB8s', uid, user_id_bytes, status, timestamp, punch, b'\x00' * 8)
                
                total_size = len(att_data)
                full_data = struct.pack('I', total_size) + att_data
                response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
            else:
                # For other types, return empty data
                response_data = struct.pack('I', 0)  # 0 bytes to follow
                response = self.create_header(CMD_DATA, session_id, reply_id, response_data)
        else:
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        
        return response
    
    def handle_reg_event(self, session_id, reply_id, data):
        """Handle register event"""
        if len(data) >= 4:
            flags = struct.unpack('I', data[:4])[0]
            print(f"  -> Handling CMD_REG_EVENT: flags={flags:#x}")
            self.registered_events = flags
        else:
            print("  -> Handling CMD_REG_EVENT: clearing events")
            self.registered_events = 0
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_start_verify(self, session_id, reply_id):
        """Handle start verify"""
        print("  -> Handling CMD_STARTVERIFY")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_options_wrq(self, session_id, reply_id, data):
        """Handle options write request"""
        option_str = data.rstrip(b'\x00').decode('utf-8', errors='ignore')
        print(f"  -> Handling CMD_OPTIONS_WRQ: {option_str}")
        
        # Handle specific options
        if '=' in option_str:
            key, value = option_str.split('=', 1)
            if key == 'SDKBuild':
                print(f"     Setting SDKBuild to {value}")
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_get_pin_width(self, session_id, reply_id):
        """Handle get pin width"""
        print("  -> Handling CMD_GET_PINWIDTH")
        # Return pin width (typically 5 or 9)
        # Client sends ' P' and expects a single byte back with the width
        response_data = b'\x05'  # 5 digit pin width
        response = self.create_header(CMD_ACK_OK, session_id, reply_id, response_data)
        return response
    
    def handle_unlock(self, session_id, reply_id, data):
        """Handle unlock door"""
        if len(data) >= 4:
            unlock_time = struct.unpack('I', data[:4])[0] / 10
            print(f"  -> Handling CMD_UNLOCK: {unlock_time} seconds")
        else:
            print("  -> Handling CMD_UNLOCK")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_test_voice(self, session_id, reply_id, data):
        """Handle test voice"""
        if len(data) >= 4:
            voice_index = struct.unpack('I', data[:4])[0]
            print(f"  -> Handling CMD_TESTVOICE: index={voice_index}")
        else:
            print("  -> Handling CMD_TESTVOICE")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_refresh_data(self, session_id, reply_id):
        """Handle refresh data"""
        print("  -> Handling CMD_REFRESHDATA")
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_user_wrq(self, session_id, reply_id, data):
        """Handle user write request - set or update user"""
        print("  -> Handling CMD_USER_WRQ (set user)")
        
        # Parse user data based on packet size
        if len(data) >= 28:
            # ZK6 format (28 bytes)
            uid, privilege, password, name, card, group_id, tz, user_id = struct.unpack('<HB5s8sIxBHI', data[:28])
            password = password.rstrip(b'\x00')
            name = name.rstrip(b'\x00')
            print(f"     User: uid={uid}, name={name}, privilege={privilege}")
            
            # Update or add user
            user_found = False
            new_users = []
            for u in self.users:
                if u[0] == uid:  # uid match
                    new_users.append((uid, privilege, password, name, card, group_id, user_id))
                    user_found = True
                else:
                    new_users.append(u)
            
            if not user_found:
                new_users.append((uid, privilege, password, name, card, group_id, user_id))
            
            self.users = new_users
            self.users_count = len(self.users)
        elif len(data) >= 72:
            # ZK8 format (72 bytes)
            uid, privilege, password, name, card, group_id, user_id = struct.unpack('<HB8s24sIx7sx24s', data[:72])
            password = password.rstrip(b'\x00')
            name = name.rstrip(b'\x00')
            user_id_str = user_id.rstrip(b'\x00')
            print(f"     User: uid={uid}, name={name}, privilege={privilege}, user_id={user_id_str}")
            
            # Update or add user
            user_found = False
            new_users = []
            for u in self.users:
                if u[0] == uid:
                    new_users.append((uid, privilege, password, name, card, 0, int(user_id_str) if user_id_str.isdigit() else uid))
                    user_found = True
                else:
                    new_users.append(u)
            
            if not user_found:
                new_users.append((uid, privilege, password, name, card, 0, int(user_id_str) if user_id_str.isdigit() else uid))
            
            self.users = new_users
            self.users_count = len(self.users)
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_delete_user(self, session_id, reply_id, data):
        """Handle delete user"""
        if len(data) >= 2:
            uid = struct.unpack('<H', data[:2])[0]
            print(f"  -> Handling CMD_DELETE_USER: uid={uid}")
            
            # Remove user
            self.users = [u for u in self.users if u[0] != uid]
            # Remove user's templates
            self.templates = [t for t in self.templates if t[0] != uid]
            self.users_count = len(self.users)
            self.fingers_count = len(self.templates)
        else:
            print("  -> Handling CMD_DELETE_USER")
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_delete_usertemp(self, session_id, reply_id, data):
        """Handle delete user template"""
        if len(data) >= 3:
            uid, temp_id = struct.unpack('<Hb', data[:3])
            print(f"  -> Handling CMD_DELETE_USERTEMP: uid={uid}, fid={temp_id}")
            
            # Remove specific template
            self.templates = [t for t in self.templates if not (t[0] == uid and t[1] == temp_id)]
            self.fingers_count = len(self.templates)
        else:
            print("  -> Handling CMD_DELETE_USERTEMP")
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_get_usertemp(self, session_id, reply_id, data):
        """Handle get user template (single)"""
        if len(data) >= 3:
            uid, temp_id = struct.unpack('<Hb', data[:3])
            print(f"  -> Handling _CMD_GET_USERTEMP: uid={uid}, fid={temp_id}")
            
            # Find template
            for t in self.templates:
                if t[0] == uid and t[1] == temp_id:
                    # Return template data
                    template_data = t[3] if len(t) > 3 else b'\x00' * 512
                    response = self.create_header(CMD_DATA, session_id, reply_id, template_data + b'\x00\x00\x00\x00\x00\x00')
                    return response
            
            # Template not found - return empty
            print("     Template not found")
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        else:
            print("  -> Handling _CMD_GET_USERTEMP")
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        
        return response
    
    def handle_db_rrq(self, session_id, reply_id, data):
        """Handle database read request (for templates)"""
        print("  -> Handling CMD_DB_RRQ")
        
        # Return template data
        template_data = b''
        for uid, fid, valid, tmpl in self.templates:
            size = len(tmpl) + 6
            template_data += struct.pack('<HHbb', size, uid, fid, valid) + tmpl
        
        total_size = len(template_data)
        full_data = struct.pack('I', total_size) + template_data
        
        response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
        return response
    
    def handle_attlog_rrq(self, session_id, reply_id):
        """Handle attendance log read request"""
        print("  -> Handling CMD_ATTLOG_RRQ")
        
        # Return attendance records
        # Format: 40 bytes per record for modern devices
        att_data = b''
        for user_id, timestamp, status, punch, uid in self.attendance_records:
            # 40-byte format: uid(2) + user_id(24) + status(1) + timestamp(4) + punch(1) + space(8)
            user_id_bytes = str(user_id).encode()[:24].ljust(24, b'\x00')
            att_data += struct.pack('<H24sB4sB8s', uid, user_id_bytes, status, timestamp, punch, b'\x00' * 8)
        
        total_size = len(att_data)
        full_data = struct.pack('I', total_size) + att_data
        
        response = self.create_header(CMD_DATA, session_id, reply_id, full_data)
        return response
    
    def handle_start_enroll(self, session_id, reply_id, data):
        """Handle start enrollment"""
        print("  -> Handling CMD_STARTENROLL")
        
        if len(data) >= 26:
            # TCP format: user_id(24) + temp_id(1) + flag(1)
            user_id, temp_id, flag = struct.unpack('<24sbb', data[:26])
            user_id = user_id.rstrip(b'\x00').decode(errors='ignore')
            print(f"     Enrolling: user_id={user_id}, temp_id={temp_id}, flag={flag}")
            
            # Find uid from user_id
            uid = None
            for u in self.users:
                u_user_id = str(u[6]) if isinstance(u[6], int) else u[6].decode(errors='ignore') if isinstance(u[6], bytes) else str(u[6])
                if u_user_id == user_id:
                    uid = u[0]
                    break
            if uid is None:
                try:
                    uid = int(user_id)
                except:
                    uid = 1
            
            self.enrollment_mode = True
            self.enrollment_uid = uid
            self.enrollment_fid = temp_id
            print(f"     Enrollment mode activated for uid={uid}, fid={temp_id}")
        elif len(data) >= 5:
            # UDP format: user_id(4) + temp_id(1)
            user_id, temp_id = struct.unpack('<Ib', data[:5])
            print(f"     Enrolling: user_id={user_id}, temp_id={temp_id}")
            self.enrollment_mode = True
            self.enrollment_uid = user_id
            self.enrollment_fid = temp_id
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def simulate_enrollment_events(self, conn, session_id):
        """Simulate fingerprint enrollment by sending event packets"""
        print("  -> Starting enrollment simulation in background")
        time.sleep(0.5)  # Small delay before starting
        
        # Simulate 3 finger scans (attempts)
        for attempt in range(3):
            print(f"  -> Simulating finger scan attempt {attempt + 1}/3")
            
            # Send first event - finger placed
            event_data = struct.pack('<H', 1)  # Event code 1 = finger placed
            event_packet = self.create_header(CMD_REG_EVENT, session_id, 0, event_data)
            if self.use_tcp:
                event_packet = self.create_tcp_top(event_packet)
            
            try:
                conn.send(event_packet)
                print(f"  -> Sent finger placed event")
                
                # Wait for ACK
                ack = conn.recv(1024)
                print(f"  -> Received ACK for first event")
                
                time.sleep(0.3)
                
                # Send second event - continue (0x64 = 100)
                event_data = struct.pack('<H', 0x64)  # Event code 0x64 = place finger again
                event_packet = self.create_header(CMD_REG_EVENT, session_id, 0, event_data)
                if self.use_tcp:
                    event_packet = self.create_tcp_top(event_packet)
                
                conn.send(event_packet)
                print(f"  -> Sent continue event (0x64)")
                
                # Wait for ACK
                ack = conn.recv(1024)
                print(f"  -> Received ACK for second event")
                
                time.sleep(0.5)
                
            except Exception as e:
                print(f"  -> Error sending enrollment events: {e}")
                return
        
        # After 3 successful scans, send final success event
        print("  -> Sending final enrollment success event")
        
        # Create a dummy fingerprint template
        template_data = b'\x00' * 512  # Simple 512-byte template
        template_size = len(template_data)
        
        # Send final event with res=0 (success), size, and position
        event_data = struct.pack('<HHH', 0, template_size, self.enrollment_fid)
        event_packet = self.create_header(CMD_REG_EVENT, session_id, 0, event_data)
        if self.use_tcp:
            event_packet = self.create_tcp_top(event_packet)
        
        try:
            conn.send(event_packet)
            print(f"  -> Sent enrollment success event")
            
            # Wait for ACK
            ack = conn.recv(1024)
            print(f"  -> Received ACK for success event")
            
            # Store the fingerprint template
            # Remove old template if exists
            self.templates = [t for t in self.templates if not (t[0] == self.enrollment_uid and t[1] == self.enrollment_fid)]
            # Add new template
            self.templates.append((self.enrollment_uid, self.enrollment_fid, 1, template_data))
            self.fingers_count = len(self.templates)
            
            print(f"  -> Enrollment complete! Template stored for uid={self.enrollment_uid}, fid={self.enrollment_fid}")
            
        except Exception as e:
            print(f"  -> Error sending final enrollment event: {e}")
        
        self.enrollment_mode = False
    
    def handle_cancel_capture(self, session_id, reply_id):
        """Handle cancel capture"""
        print("  -> Handling CMD_CANCELCAPTURE")
        self.enrollment_mode = False
        self.enrollment_uid = 0
        self.enrollment_fid = 0
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_prepare_data(self, session_id, reply_id, data):
        """Handle prepare data - initialize buffer for upload"""
        if len(data) >= 4:
            size = struct.unpack('I', data[:4])[0]
            print(f"  -> Handling CMD_PREPARE_DATA: buffer size={size}")
            self.data_buffer = b''
            response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        else:
            print("  -> Handling CMD_PREPARE_DATA: invalid size")
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        return response
    
    def handle_data_packet(self, session_id, reply_id, data):
        """Handle data packet - accumulate data in buffer"""
        print(f"  -> Handling CMD_DATA: received {len(data)} bytes")
        self.data_buffer += data
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_save_usertemps(self, session_id, reply_id, data):
        """Handle save user templates - process buffered user and template data"""
        print(f"  -> Handling _CMD_SAVE_USERTEMPS")
        
        if len(data) >= 10:
            # Parse command parameters
            size1, cmd, fct = struct.unpack('<IHH', data[:10])
            print(f"     Parameters: size1={size1}, cmd={cmd}, fct={fct}")
        
        # Process the buffered data
        if len(self.data_buffer) >= 12:
            upack_size, table_size, fpack_size = struct.unpack('III', self.data_buffer[:12])
            print(f"     Buffer: upack_size={upack_size}, table_size={table_size}, fpack_size={fpack_size}")
            
            offset = 12
            
            # Parse user data
            if upack_size > 0:
                user_data = self.data_buffer[offset:offset+upack_size]
                offset += upack_size
                
                # Determine user packet size (28 or 72 bytes)
                if upack_size >= 72 and upack_size % 72 == 0:
                    packet_size = 72
                elif upack_size >= 28:
                    packet_size = 28
                else:
                    packet_size = 28
                
                # Parse users
                while len(user_data) >= packet_size:
                    if packet_size == 72:
                        uid, privilege, password, name, card, group_id, user_id = struct.unpack('<HB8s24sIx7sx24s', user_data[:72])
                        password = password.rstrip(b'\x00')
                        name = name.rstrip(b'\x00')
                        user_id = user_id.rstrip(b'\x00')
                        group_id = group_id.rstrip(b'\x00')
                        print(f"     User: uid={uid}, name={name}, user_id={user_id}")
                        
                        # Update or add user
                        user_found = False
                        new_users = []
                        for u in self.users:
                            if u[0] == uid:
                                new_users.append((uid, privilege, password, name, card, 0, int(user_id) if user_id.isdigit() else uid))
                                user_found = True
                            else:
                                new_users.append(u)
                        
                        if not user_found:
                            new_users.append((uid, privilege, password, name, card, 0, int(user_id) if user_id.isdigit() else uid))
                        
                        self.users = new_users
                        user_data = user_data[72:]
                    else:  # packet_size == 28
                        uid, privilege, password, name, card, group_id, tz, user_id = struct.unpack('<HB5s8sIxBHI', user_data[:28])
                        password = password.rstrip(b'\x00')
                        name = name.rstrip(b'\x00')
                        print(f"     User: uid={uid}, name={name}, user_id={user_id}")
                        
                        # Update or add user
                        user_found = False
                        new_users = []
                        for u in self.users:
                            if u[0] == uid:
                                new_users.append((uid, privilege, password, name, card, group_id, user_id))
                                user_found = True
                            else:
                                new_users.append(u)
                        
                        if not user_found:
                            new_users.append((uid, privilege, password, name, card, group_id, user_id))
                        
                        self.users = new_users
                        user_data = user_data[28:]
                
                self.users_count = len(self.users)
            
            # Parse template table
            templates_to_add = []
            if table_size > 0:
                table_data = self.data_buffer[offset:offset+table_size]
                offset += table_size
                
                # Parse fingerprint data
                fpack_data = self.data_buffer[offset:offset+fpack_size]
                
                # Parse table entries (8 bytes each)
                fdata_offset = 0
                while len(table_data) >= 8:
                    entry_type, uid, fnum, tstart = struct.unpack('<bHbI', table_data[:8])
                    table_data = table_data[8:]
                    
                    # Extract template from fpack
                    if entry_type == 2:  # fingerprint template
                        fid = fnum - 0x10  # Convert from internal format
                        # Find template size (need to check next entry or use remaining data)
                        if len(table_data) >= 8:
                            next_entry = struct.unpack('<bHbI', table_data[:8])
                            tend = next_entry[3]
                        else:
                            tend = len(fpack_data)
                        
                        template_data = fpack_data[tstart:tend]
                        print(f"     Template: uid={uid}, fid={fid}, size={len(template_data)}")
                        templates_to_add.append((uid, fid, 1, template_data))
            
            # Add templates
            for tmpl in templates_to_add:
                # Remove old template if exists
                self.templates = [t for t in self.templates if not (t[0] == tmpl[0] and t[1] == tmpl[1])]
                # Add new template
                self.templates.append(tmpl)
            
            self.fingers_count = len(self.templates)
        
        # Clear buffer
        self.data_buffer = b''
        
        response = self.create_header(CMD_ACK_OK, session_id, reply_id)
        return response
    
    def handle_read_buffer(self, session_id, reply_id, data):
        """Handle read buffer - read chunk of data from buffer"""
        if len(data) >= 8:
            start, size = struct.unpack('<ii', data[:8])
            print(f"  -> Handling _CMD_READ_BUFFER: start={start}, size={size}")
            
            # This would normally return a chunk of buffered data
            # For now, just return empty data
            response_data = struct.pack('I', 0) + b''
            response = self.create_header(CMD_DATA, session_id, reply_id, response_data)
        else:
            print("  -> Handling _CMD_READ_BUFFER: invalid parameters")
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        return response

    
    def handle_packet(self, packet):
        """Handle incoming packet"""
        if self.use_tcp:
            # Parse TCP header
            if len(packet) < 8:
                print(f"  ! TCP packet too short: {len(packet)} bytes")
                return None, False
            
            tcp_header = struct.unpack('<HHI', packet[:8])
            print(f"  TCP header: magic1=0x{tcp_header[0]:04x}, magic2=0x{tcp_header[1]:04x}, length={tcp_header[2]}")
            if tcp_header[0] != MACHINE_PREPARE_DATA_1 or tcp_header[1] != MACHINE_PREPARE_DATA_2:
                print(f"  ! Invalid TCP header (expected 0x{MACHINE_PREPARE_DATA_1:04x}/0x{MACHINE_PREPARE_DATA_2:04x})")
                return None, False
            
            packet = packet[8:]  # Remove TCP header
        
        if len(packet) < 8:
            return None, False
        
        # Parse packet header
        header = struct.unpack('<4H', packet[:8])
        command = header[0]
        session_id = header[2]
        reply_id = header[3]
        data = packet[8:]
        
        print(f"\n[Received] Command: {command} (0x{command:04x}), Session: {session_id}, Reply: {reply_id}")
        
        response = None
        start_enrollment = False
        
        # Route to appropriate handler
        if command == CMD_CONNECT:
            response = self.handle_connect(session_id, reply_id)
        elif command == CMD_AUTH:
            response = self.handle_auth(session_id, reply_id, data)
        elif command == CMD_EXIT:
            response = self.handle_exit(session_id, reply_id)
        elif command == CMD_ENABLEDEVICE:
            response = self.handle_enable_device(session_id, reply_id)
        elif command == CMD_DISABLEDEVICE:
            response = self.handle_disable_device(session_id, reply_id)
        elif command == CMD_GET_VERSION:
            response = self.handle_get_version(session_id, reply_id)
        elif command == CMD_GET_TIME:
            response = self.handle_get_time(session_id, reply_id)
        elif command == CMD_SET_TIME:
            response = self.handle_set_time(session_id, reply_id, data)
        elif command == CMD_OPTIONS_RRQ:
            response = self.handle_options_rrq(session_id, reply_id, data)
        elif command == CMD_OPTIONS_WRQ:
            response = self.handle_options_wrq(session_id, reply_id, data)
        elif command == CMD_GET_FREE_SIZES:
            response = self.handle_get_free_sizes(session_id, reply_id)
        elif command == CMD_USERTEMP_RRQ:
            response = self.handle_usertemp_rrq(session_id, reply_id)
        elif command == CMD_FREE_DATA:
            response = self.handle_free_data(session_id, reply_id)
        elif command == CMD_PREPARE_BUFFER or command == 1503:
            response = self.handle_prepare_buffer(session_id, reply_id, data)
        elif command == CMD_REG_EVENT:
            response = self.handle_reg_event(session_id, reply_id, data)
        elif command == CMD_STARTVERIFY:
            response = self.handle_start_verify(session_id, reply_id)
        elif command == CMD_GET_PINWIDTH:
            response = self.handle_get_pin_width(session_id, reply_id)
        elif command == CMD_UNLOCK:
            response = self.handle_unlock(session_id, reply_id, data)
        elif command == CMD_TESTVOICE:
            response = self.handle_test_voice(session_id, reply_id, data)
        elif command == CMD_USER_WRQ:
            response = self.handle_user_wrq(session_id, reply_id, data)
        elif command == CMD_DELETE_USER:
            response = self.handle_delete_user(session_id, reply_id, data)
        elif command == CMD_REFRESHDATA:
            response = self.handle_refresh_data(session_id, reply_id)
        elif command == CMD_DELETE_USERTEMP:
            response = self.handle_delete_usertemp(session_id, reply_id, data)
        elif command == _CMD_GET_USERTEMP:
            response = self.handle_get_usertemp(session_id, reply_id, data)
        elif command == CMD_DB_RRQ:
            response = self.handle_db_rrq(session_id, reply_id, data)
        elif command == CMD_ATTLOG_RRQ:
            response = self.handle_attlog_rrq(session_id, reply_id)
        elif command == CMD_STARTENROLL:
            response = self.handle_start_enroll(session_id, reply_id, data)
            start_enrollment = True  # Flag to start enrollment simulation after sending response
        elif command == CMD_CANCELCAPTURE:
            response = self.handle_cancel_capture(session_id, reply_id)
        elif command == CMD_PREPARE_DATA:
            response = self.handle_prepare_data(session_id, reply_id, data)
        elif command == CMD_DATA:
            response = self.handle_data_packet(session_id, reply_id, data)
        elif command == _CMD_SAVE_USERTEMPS:
            response = self.handle_save_usertemps(session_id, reply_id, data)
        elif command == _CMD_PREPARE_BUFFER:
            response = self.handle_prepare_buffer(session_id, reply_id, data)
        elif command == _CMD_READ_BUFFER:
            response = self.handle_read_buffer(session_id, reply_id, data)
        else:
            print(f"  ! Unknown command: {command}")
            response = self.create_header(CMD_ACK_ERROR, session_id, reply_id)
        
        if response and self.use_tcp:
            response = self.create_tcp_top(response)
            tcp_header = struct.unpack('<HHI', response[:8])
            print(f"[Prepared response] {len(response)} bytes total (8-byte TCP header + {len(response)-8}-byte payload)")
            print(f"  Response TCP header: magic1=0x{tcp_header[0]:04x}, magic2=0x{tcp_header[1]:04x}, length={tcp_header[2]}")
            print(f"  First 16 bytes (hex): {response[:16].hex()}")
        
        return response, start_enrollment
    
    def run(self):
        """Run the simulator server"""
        if self.use_tcp:
            sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            protocol_name = "TCP"
        else:
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            protocol_name = "UDP"
        
        try:
            sock.bind((self.ip, self.port))
            print(f"ZKTeco Simulator running on {self.ip}:{self.port} ({protocol_name})")
            print(f"Password: {self.password}")
            print(f"Simulated users: {len(self.users)}")
            print("\nWaiting for connections...\n")
            
            if self.use_tcp:
                sock.listen(5)
                
                while True:
                    conn, addr = sock.accept()
                    print(f"\n{'='*60}")
                    print(f"Connection from {addr}")
                    print('='*60)
                    
                    try:
                        enrollment_session_id = None
                        while True:
                            data = conn.recv(1024)
                            if not data:
                                break
                            
                            response, start_enrollment = self.handle_packet(data)
                            if response:
                                conn.send(response)
                                print(f"[Sent] Response: {len(response)} bytes")
                                
                                # If enrollment was initiated, start background simulation
                                if start_enrollment:
                                    enrollment_session_id = self.session_id
                                    thread = threading.Thread(
                                        target=self.simulate_enrollment_events, 
                                        args=(conn, enrollment_session_id)
                                    )
                                    thread.daemon = True
                                    thread.start()
                    except Exception as e:
                        print(f"Error handling connection: {e}")
                        import traceback
                        traceback.print_exc()
                    finally:
                        conn.close()
                        print(f"\nConnection closed from {addr}\n")
            else:
                # UDP mode
                while True:
                    data, addr = sock.recvfrom(1024)
                    print(f"\n{'='*60}")
                    print(f"Packet from {addr}")
                    print('='*60)
                    
                    response, start_enrollment = self.handle_packet(data)
                    if response:
                        sock.sendto(response, addr)
                        print(f"[Sent] Response: {len(response)} bytes")
                    
                    # Note: UDP enrollment is not fully supported in this simulator
                    if start_enrollment:
                        print("  ! Warning: Enrollment simulation not supported in UDP mode")
        
        except KeyboardInterrupt:
            print("\n\nShutting down simulator...")
        finally:
            sock.close()
            print("Simulator stopped.")


def main():
    """Main entry point"""
    import argparse
    
    parser = argparse.ArgumentParser(description='ZKTeco Device Simulator')
    parser.add_argument('--ip', default='0.0.0.0', help='IP address to bind (default: 0.0.0.0)')
    parser.add_argument('--port', type=int, default=4370, help='Port to bind (default: 4370)')
    parser.add_argument('--password', type=int, default=0, help='Device password (default: 0 - no password)')
    parser.add_argument('--udp', action='store_true', help='Use UDP instead of TCP')
    
    args = parser.parse_args()
    
    simulator = ZKSimulator(
        ip=args.ip,
        port=args.port,
        password=args.password,
        use_tcp=not args.udp
    )
    
    simulator.run()


if __name__ == '__main__':
    main()
