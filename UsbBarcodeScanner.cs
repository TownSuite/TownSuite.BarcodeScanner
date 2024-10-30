using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace BasselTech
{
    namespace UsbBarcodeScanner
    {
        public class BarcodeScannedEventArgs : EventArgs
        {
            public BarcodeScannedEventArgs(string barcode)
            {
                Barcode = barcode;
            }

            public string Barcode { get; }
        }

        public class UsbBarcodeScanner
        {
            #region WinAPI Declarations

            [DllImport("kernel32.dll")]
            private static extern IntPtr LoadLibrary(string lpLibFileName);

            [DllImport("user32.dll")]
            private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hInstance, uint threadId);

            [DllImport("user32.dll")]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll")]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll")]
            private static extern IntPtr GetKeyboardLayout(uint idThread);

            [DllImport("user32.dll")]
            static extern bool GetKeyboardState(byte[] lpKeyState);

            [DllImport("user32.dll")]
            static extern uint MapVirtualKey(uint uCode, uint uMapType);

            [DllImport("user32.dll")]
            static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

            [DllImport("setupapi.dll", SetLastError = true)]
            private static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

            [DllImport("setupapi.dll", SetLastError = true)]
            private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

            [DllImport("setupapi.dll", SetLastError = true)]
            private static extern bool SetupDiGetDeviceInstanceId(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, StringBuilder DeviceInstanceId, int DeviceInstanceIdSize, out int RequiredSize);

            #endregion

            #region Delegates and Constants

            private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
            private const int WH_KEYBOARD_LL = 13;
            private const int WM_KEYDOWN = 0x0100;
            private const int WM_KEYUP = 0x0101;
            private const uint DIGCF_PRESENT = 0x00000002;
            private const uint DIGCF_DEVICEINTERFACE = 0x00000010;

            #endregion

            #region Private Fields

            private IntPtr _hookId = IntPtr.Zero;
            private readonly List<Keys> _keys = new List<Keys>();
            private readonly Timer _timer = new Timer();
            private readonly Keys _endKey;
            private readonly string _endKeyStr;
            private readonly string _targetDeviceId;
            private LowLevelKeyboardProc _keyboardProc;

            #endregion

            #region Events

            public event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

            #endregion

            #region Constructor

            public UsbBarcodeScanner() : this(Keys.Enter, string.Empty)
            {
            }

            public UsbBarcodeScanner(Keys endKey, string targetDeviceId)
            {
                _targetDeviceId = targetDeviceId;
                _endKey = endKey;
                _timer.Interval = 20;
                _timer.Tick += (sender, args) => _keys.Clear();
                _timer.Stop();

                _keyboardProc = KeyboardHookCallback;
            }

            public UsbBarcodeScanner(string endKey, string targetDeviceId)
            {
                _targetDeviceId = targetDeviceId;
                _endKeyStr = endKey;
                _timer.Interval = 20;
                _timer.Tick += (sender, args) => _keys.Clear();
                _timer.Stop();

                _keyboardProc = KeyboardHookCallback;
            }


            public void Start()
            {
                if (IsCapturing())
                    return;
                _hookId = SetHook();
                _timer.Start();
            }

            public void Stop()
            {
                if (!IsCapturing())
                    return;
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
                _timer.Stop();
                _keys.Clear();
            }

            public bool IsCapturing()
            {
                return _hookId != IntPtr.Zero;
            }

            #endregion

            #region Private Methods

            private IntPtr SetHook()
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, LoadLibrary("user32"), 0);
                }
            }

            private string GetBarcodeString()
            {
                var barcodeBuilder = new StringBuilder();
                var shiftFlag = false;

                foreach (var key in _keys)
                {
                    if (key == Keys.ShiftKey || key == Keys.LShiftKey || key == Keys.RShiftKey)
                    {
                        shiftFlag = true;
                        continue;
                    }

                    barcodeBuilder.Append(KeyCodeToUnicode(key, shiftFlag));
                    shiftFlag = false;
                }

                // Return all but the last character (end condition)
                if (barcodeBuilder.Length > 0)
                {
                    barcodeBuilder.Length -= 1;
                }

                return barcodeBuilder.ToString();
            }

            private string KeyCodeToUnicode(Keys key, bool shiftFlag)
            {
                var lpKeyState = new byte[255];
                GetKeyboardState(lpKeyState);

                // Explicitly set the Shift key state based on the shiftFlag parameter
                // as the GetKeyboardState method does not return the correct state for the Shift key
                if (shiftFlag)
                {
                    lpKeyState[(int)Keys.ShiftKey] = 0x80;
                }
                else
                {
                    lpKeyState[(int)Keys.ShiftKey] = 0x0;
                }

                var wVirtKey = (uint)key;
                var wScanCode = MapVirtualKey(wVirtKey, 0);

                var pwszBuff = new StringBuilder();
                ToUnicodeEx(wVirtKey, wScanCode, lpKeyState, pwszBuff, 5, 0, GetKeyboardLayout(0));

                return pwszBuff.ToString();
            }

            private bool shouldRaiseEvent = false;
            private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    // Extract the virtual key code
                    var vkCode = Marshal.ReadInt32(lParam);

                    // Convert the virtual key code to a Keys enum value
                    var key = (Keys)vkCode;

                    // Check if the key event is from the target device
                    if (IsKeyFromTargetDevice())
                    {
                        _timer.Stop();
                        shouldRaiseEvent = EndKeyDetected(key);
                        if (!shouldRaiseEvent)
                        {
                            _keys.Add(key);
                            _timer.Start();
                        }
                    }
                }
                else if (shouldRaiseEvent && nCode >= 0 && wParam == (IntPtr)WM_KEYUP)
                {
                    shouldRaiseEvent = false;
                    if (_keys.Count > 0)
                    {
                        var barcode = GetBarcodeString();
                        BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(barcode));
                    }
                    _keys.Clear();
                }

                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            private bool EndKeyDetected(Keys key)
            {
                bool shiftFlag = false;
                if (_keys.Count > 0)
                {
                    Keys previousKey = _keys[_keys.Count - 1];
                    if (previousKey == Keys.ShiftKey || previousKey == Keys.LShiftKey || previousKey == Keys.RShiftKey)
                    {
                        shiftFlag = true;
                    }
                }

                string keyStr = KeyCodeToUnicode(key, shiftFlag);

                if (key == _endKey || (!string.IsNullOrEmpty(_endKeyStr) && string.Equals(keyStr, _endKeyStr)))
                {
                    return true;
                }

                return false;
            }

            private bool IsKeyFromTargetDevice()
            {
                if (string.IsNullOrEmpty(_targetDeviceId))
                {
                    return true;
                }

                // HID Class GUID: This specific GUID is predefined by Microsoft to represent the HID device class
                var hidGuid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");
                var deviceInfoSet = SetupDiGetClassDevs(ref hidGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);

                if (deviceInfoSet == IntPtr.Zero)
                {
                    return false;
                }

                var deviceInfoData = new SP_DEVINFO_DATA();
                deviceInfoData.cbSize = Marshal.SizeOf(deviceInfoData);

                for (uint i = 0; SetupDiEnumDeviceInfo(deviceInfoSet, i, ref deviceInfoData); i++)
                {
                    var deviceInstanceId = new StringBuilder(256);
                    if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, deviceInstanceId, deviceInstanceId.Capacity, out _))
                    {
                        var normalizedDeviceInstanceId = deviceInstanceId.ToString().Replace('\\', '#');
                        if (normalizedDeviceInstanceId.Contains(_targetDeviceId))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            #endregion

            #region Destructor

            ~UsbBarcodeScanner()
            {
                Stop();
            }

            #endregion

            #region Structs

            [StructLayout(LayoutKind.Sequential)]
            private struct SP_DEVINFO_DATA
            {
                public int cbSize;
                public Guid ClassGuid;
                public uint DevInst;
                public IntPtr Reserved;
            }

            #endregion
        }
    }
}
