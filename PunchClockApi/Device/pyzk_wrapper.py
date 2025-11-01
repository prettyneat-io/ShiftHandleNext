# -*- coding: utf-8 -*-
"""
PyZK .NET Wrapper
=================
This module provides a simplified, .NET-friendly wrapper around the pyzk library.
It handles common operations and provides better error handling for interop scenarios.
"""

import json
from datetime import datetime
from typing import List, Dict, Optional, Any
import sys
import os

# Add parent directory to path to import zk
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '..')))

from zk import ZK, const
from zk.user import User
from zk.attendance import Attendance
from zk.finger import Finger


class PyZKWrapper:
    """
    A .NET-friendly wrapper for pyzk library operations.
    Provides simplified methods with JSON serialization for easy interop.
    """
    
    def __init__(self, ip_address: str, port: int = 4370, timeout: int = 5, 
                 password: int = 0, force_udp: bool = False, ommit_ping: bool = False):
        """
        Initialize the ZK device connection wrapper.
        
        Args:
            ip_address: IP address of the ZK device
            port: Port number (default: 4370)
            timeout: Connection timeout in seconds (default: 5)
            password: Device password (default: 0)
            force_udp: Force UDP communication (default: False)
            ommit_ping: Skip ping test (default: False)
        """
        self.ip_address = ip_address
        self.port = port
        self.timeout = timeout
        self.password = password
        self.force_udp = force_udp
        self.ommit_ping = ommit_ping
        
        self.zk = ZK(ip_address, port=port, timeout=timeout, password=password,
                     force_udp=force_udp, ommit_ping=ommit_ping)
        self.conn = None
        self._is_connected = False
    
    @property
    def is_connected(self) -> bool:
        """Check if currently connected to device."""
        return self._is_connected
    
    def connect(self) -> Dict[str, Any]:
        """
        Connect to the ZK device.
        
        Returns:
            Dictionary with connection status and device info
        """
        try:
            self.conn = self.zk.connect()
            self._is_connected = True
            
            # Get basic device info
            return {
                "success": True,
                "message": "Connected successfully",
                "device_info": {
                    "firmware_version": self.conn.get_firmware_version(),
                    "serial_number": self.conn.get_serialnumber(),
                    "platform": self.conn.get_platform(),
                    "device_name": self.conn.get_device_name()
                }
            }
        except Exception as e:
            self._is_connected = False
            return {
                "success": False,
                "message": f"Connection failed: {str(e)}",
                "error": str(e)
            }
    
    def disconnect(self) -> Dict[str, bool]:
        """
        Disconnect from the ZK device.
        
        Returns:
            Dictionary with disconnect status
        """
        try:
            if self.conn:
                self.conn.disconnect()
            self._is_connected = False
            return {"success": True, "message": "Disconnected successfully"}
        except Exception as e:
            return {"success": False, "message": str(e)}
    
    def get_users_json(self) -> str:
        """
        Get all users from the device as JSON string.
        
        Returns:
            JSON string containing list of users
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            users = self.conn.get_users()
            
            users_list = []
            for user in users:
                users_list.append({
                    "uid": user.uid,
                    "name": user.name,
                    "privilege": user.privilege,
                    "privilege_name": "Admin" if user.privilege == const.USER_ADMIN else "User",
                    "password": user.password,
                    "group_id": user.group_id,
                    "user_id": user.user_id,
                    "card": user.card
                })
            
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "count": len(users_list),
                "users": users_list
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def get_attendance_json(self) -> str:
        """
        Get all attendance records from the device as JSON string.
        
        Returns:
            JSON string containing list of attendance records
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            attendances = self.conn.get_attendance()
            
            attendance_list = []
            for att in attendances:
                attendance_list.append({
                    "uid": att.uid,
                    "user_id": att.user_id,
                    "timestamp": att.timestamp.isoformat() if att.timestamp else None,
                    "status": att.status,
                    "punch": att.punch
                })
            
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "count": len(attendance_list),
                "attendances": attendance_list
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def add_user(self, uid: int, name: str, privilege: int = const.USER_DEFAULT, 
                 password: str = '', group_id: str = '', user_id: str = '', 
                 card: int = 0) -> str:
        """
        Add a new user to the device.
        
        Args:
            uid: Unique user identifier
            name: User name
            privilege: User privilege level (0=User, 14=Admin)
            password: User password
            group_id: Group identifier
            user_id: User ID string
            card: Card number
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            self.conn.set_user(uid=uid, name=name, privilege=privilege, 
                              password=password, group_id=group_id, 
                              user_id=user_id, card=card)
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "message": f"User {name} (UID: {uid}) added successfully"
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def delete_user(self, uid: int = None, user_id: str = None) -> str:
        """
        Delete a user from the device.
        
        Args:
            uid: User UID (optional if user_id provided)
            user_id: User ID string (optional if uid provided)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        if uid is None and user_id is None:
            return json.dumps({"success": False, "error": "Either uid or user_id must be provided"})
        
        try:
            self.conn.disable_device()
            if uid is not None:
                self.conn.delete_user(uid=uid)
            else:
                self.conn.delete_user(user_id=user_id)
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "message": f"User deleted successfully"
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def get_device_info_json(self) -> str:
        """
        Get comprehensive device information as JSON string.
        
        Returns:
            JSON string containing device information
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            info = {
                "success": True,
                "firmware_version": self.conn.get_firmware_version(),
                "serial_number": self.conn.get_serialnumber(),
                "platform": self.conn.get_platform(),
                "device_name": self.conn.get_device_name(),
                "mac_address": self.conn.get_mac(),
                "device_time": self.conn.get_time().isoformat() if self.conn.get_time() else None,
            }
            
            # Get device capacity
            self.conn.read_sizes()
            info.update({
                "users_count": self.conn.users,
                "users_capacity": self.conn.users_cap,
                "fingerprints_count": self.conn.fingers,
                "fingerprints_capacity": self.conn.fingers_cap,
                "records_count": self.conn.records
            })
            
            return json.dumps(info)
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def set_time(self, timestamp: str = None) -> str:
        """
        Set device time.
        
        Args:
            timestamp: ISO format timestamp string (default: current time)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            if timestamp:
                new_time = datetime.fromisoformat(timestamp)
            else:
                new_time = datetime.now()
            
            self.conn.set_time(new_time)
            
            return json.dumps({
                "success": True,
                "message": f"Time set to {new_time.isoformat()}"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def clear_attendance(self) -> str:
        """
        Clear all attendance records from the device.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            self.conn.clear_attendance()
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "message": "Attendance records cleared successfully"
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def test_voice(self, index: int = 0) -> str:
        """
        Play a test voice on the device.
        
        Args:
            index: Voice index (0=Thank You, see documentation for others)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.test_voice(index=index)
            return json.dumps({
                "success": True,
                "message": f"Voice test {index} played"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def restart_device(self) -> str:
        """
        Restart the device.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.restart()
            return json.dumps({
                "success": True,
                "message": "Device restart command sent"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def poweroff_device(self) -> str:
        """
        Power off the device.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.poweroff()
            return json.dumps({
                "success": True,
                "message": "Device poweroff command sent"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def unlock_door(self, time: int = 3) -> str:
        """
        Unlock the door for specified time.
        
        Args:
            time: Time in seconds to keep door unlocked (default: 3)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.unlock(time=time)
            return json.dumps({
                "success": True,
                "message": f"Door unlocked for {time} seconds"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_lock_state(self) -> str:
        """
        Get the current door lock state.
        
        Returns:
            JSON string with lock state
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            state = self.conn.get_lock_state()
            return json.dumps({
                "success": True,
                "locked": state
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def write_lcd(self, line_number: int, text: str) -> str:
        """
        Write text to device LCD screen.
        
        Args:
            line_number: Line number on LCD (0-based)
            text: Text to display
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.write_lcd(line_number, text)
            return json.dumps({
                "success": True,
                "message": f"LCD line {line_number} updated"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def clear_lcd(self) -> str:
        """
        Clear the device LCD screen.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.clear_lcd()
            return json.dumps({
                "success": True,
                "message": "LCD cleared"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def refresh_data(self) -> str:
        """
        Refresh device data.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.refresh_data()
            return json.dumps({
                "success": True,
                "message": "Device data refreshed"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def free_data(self) -> str:
        """
        Free device buffer.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.free_data()
            return json.dumps({
                "success": True,
                "message": "Device buffer freed"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_face_version(self) -> str:
        """
        Get face algorithm version.
        
        Returns:
            JSON string with face version
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            version = self.conn.get_face_version()
            return json.dumps({
                "success": True,
                "face_version": version
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_fp_version(self) -> str:
        """
        Get fingerprint algorithm version.
        
        Returns:
            JSON string with fingerprint version
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            version = self.conn.get_fp_version()
            return json.dumps({
                "success": True,
                "fingerprint_version": version
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_network_params(self) -> str:
        """
        Get device network parameters (IP, mask, gateway).
        
        Returns:
            JSON string with network parameters
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            params = self.conn.get_network_params()
            return json.dumps({
                "success": True,
                "network": params
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_pin_width(self) -> str:
        """
        Get PIN width configuration.
        
        Returns:
            JSON string with PIN width
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            width = self.conn.get_pin_width()
            return json.dumps({
                "success": True,
                "pin_width": width
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_extend_fmt(self) -> str:
        """
        Get extend format configuration.
        
        Returns:
            JSON string with extend format
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            fmt = self.conn.get_extend_fmt()
            return json.dumps({
                "success": True,
                "extend_format": fmt
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_user_extend_fmt(self) -> str:
        """
        Get user extend format configuration.
        
        Returns:
            JSON string with user extend format
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            fmt = self.conn.get_user_extend_fmt()
            return json.dumps({
                "success": True,
                "user_extend_format": fmt
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_face_fun_on(self) -> str:
        """
        Check if face function is enabled.
        
        Returns:
            JSON string with face function status
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            enabled = self.conn.get_face_fun_on()
            return json.dumps({
                "success": True,
                "face_function_enabled": bool(enabled)
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_compat_old_firmware(self) -> str:
        """
        Check firmware compatibility.
        
        Returns:
            JSON string with firmware compatibility info
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            compat = self.conn.get_compat_old_firmware()
            return json.dumps({
                "success": True,
                "old_firmware_compatible": compat
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def get_templates_json(self) -> str:
        """
        Get all fingerprint templates from the device as JSON string.
        
        Returns:
            JSON string containing list of templates
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            templates = self.conn.get_templates()
            
            templates_list = []
            for template in templates:
                templates_list.append({
                    "uid": template.uid,
                    "fid": template.fid,
                    "valid": template.valid,
                    "template_size": len(template.template) if template.template else 0
                })
            
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "count": len(templates_list),
                "templates": templates_list
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def get_user_template_json(self, uid: int = None, temp_id: int = 0, user_id: str = None) -> str:
        """
        Get a specific user's fingerprint template.
        
        Args:
            uid: User UID (optional if user_id provided)
            temp_id: Template ID (finger index 0-9, default: 0)
            user_id: User ID string (optional if uid provided)
            
        Returns:
            JSON string with template data
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            template = self.conn.get_user_template(uid=uid or 0, temp_id=temp_id, user_id=user_id or '')
            self.conn.enable_device()
            
            if template:
                return json.dumps({
                    "success": True,
                    "template": {
                        "uid": template.uid,
                        "fid": template.fid,
                        "valid": template.valid,
                        "template_size": len(template.template) if template.template else 0
                    }
                })
            else:
                return json.dumps({
                    "success": False,
                    "error": "Template not found"
                })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def delete_user_template(self, uid: int = 0, temp_id: int = 0, user_id: str = '') -> str:
        """
        Delete a specific user's fingerprint template.
        
        Args:
            uid: User UID (optional if user_id provided)
            temp_id: Template ID (finger index 0-9, default: 0)
            user_id: User ID string (optional if uid provided)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            result = self.conn.delete_user_template(uid=uid, temp_id=temp_id, user_id=user_id)
            self.conn.enable_device()
            
            if result:
                return json.dumps({
                    "success": True,
                    "message": "Template deleted successfully"
                })
            else:
                return json.dumps({
                    "success": False,
                    "error": "Template not found or could not be deleted"
                })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def enroll_user(self, uid: int = 0, temp_id: int = 0, user_id: str = '') -> str:
        """
        Start fingerprint enrollment for a user.
        
        Args:
            uid: User UID
            temp_id: Template ID (finger index 0-9, default: 0)
            user_id: User ID string
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            result = self.conn.enroll_user(uid=uid, temp_id=temp_id, user_id=user_id)
            
            if result:
                return json.dumps({
                    "success": True,
                    "message": "Enrollment completed successfully"
                })
            else:
                return json.dumps({
                    "success": False,
                    "error": "Enrollment failed"
                })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def verify_user(self) -> str:
        """
        Start user verification mode.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.verify_user()
            return json.dumps({
                "success": True,
                "message": "Verification mode started"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def cancel_capture(self) -> str:
        """
        Cancel fingerprint capture operation.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            result = self.conn.cancel_capture()
            return json.dumps({
                "success": True,
                "cancelled": result
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def reg_event(self, flags: int) -> str:
        """
        Register for device events.
        
        Args:
            flags: Event flags (see const.EF_* constants)
            
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.reg_event(flags)
            return json.dumps({
                "success": True,
                "message": f"Registered for events with flags {flags}"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def set_sdk_build_1(self) -> str:
        """
        Set SDK build to 1.
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            result = self.conn.set_sdk_build_1()
            return json.dumps({
                "success": result,
                "message": "SDK build set to 1" if result else "Failed to set SDK build"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def clear_data(self) -> str:
        """
        Clear all data from device (users, attendance, fingerprints).
        WARNING: This will delete all data!
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            self.conn.clear_data()
            self.conn.enable_device()
            
            return json.dumps({
                "success": True,
                "message": "All data cleared from device"
            })
        except Exception as e:
            if self.conn:
                self.conn.enable_device()
            return json.dumps({"success": False, "error": str(e)})
    
    def enable_device(self) -> str:
        """
        Enable device (allow user activity).
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.enable_device()
            return json.dumps({
                "success": True,
                "message": "Device enabled"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})
    
    def disable_device(self) -> str:
        """
        Disable device (lock device, prevent user activity).
        
        Returns:
            JSON string with operation result
        """
        if not self._is_connected or not self.conn:
            return json.dumps({"success": False, "error": "Not connected to device"})
        
        try:
            self.conn.disable_device()
            return json.dumps({
                "success": True,
                "message": "Device disabled"
            })
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})


# Example usage for testing
if __name__ == "__main__":
    # Test the wrapper
    wrapper = PyZKWrapper("192.168.1.201", port=4370)
    
    print("Connecting...")
    result = wrapper.connect()
    print(json.dumps(result, indent=2))
    
    if result["success"]:
        print("\nGetting device info...")
        print(wrapper.get_device_info_json())
        
        print("\nGetting users...")
        print(wrapper.get_users_json())
        
        print("\nDisconnecting...")
        print(json.dumps(wrapper.disconnect(), indent=2))
