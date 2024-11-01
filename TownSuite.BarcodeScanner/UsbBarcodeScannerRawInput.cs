using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;

namespace TownSuite.BarcodeScanner
{
    public class UsbBarcodeScannerRawInput : NativeWindow, IUsbBarcodeScanner
    {
        #region WinAPI Declarations

        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string lpLibFileName);

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

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);

        [DllImport("user32.dll")]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceInfo(IntPtr hDevice, uint uiCommand, IntPtr pData, ref uint pcbSize);


        #endregion

        #region Delegates and Constants

        private const int WM_INPUT = 0x00FF;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const uint DIGCF_PRESENT = 0x00000002;
        private const uint DIGCF_DEVICEINTERFACE = 0x00000010;
        private const uint RIDI_DEVICENAME = 0x20000007;

        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEKEYBOARD = 1;

        #endregion

        #region Private Fields

        private readonly List<Keys> _keys = new List<Keys>();
        private readonly Timer _timer = new Timer();
        private readonly Keys _endKey;
        private readonly string _endKeyStr;
        private readonly string[] _targetDeviceIds;
        private bool _isCapturing = false;
        private readonly IntPtr hwd;

        #endregion

        #region Events

        public event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        #endregion

        #region Constructor

        public UsbBarcodeScannerRawInput(Form frm) : this(frm, Keys.Enter, null)
        {
        }

        public UsbBarcodeScannerRawInput(Form frm, Keys endKey, string[] targetDeviceIds)
        {
            hwd = frm.Handle;
            _targetDeviceIds = targetDeviceIds;
            _endKey = endKey;
            _timer.Interval = 20;
            _timer.Tick += (sender, args) => _keys.Clear();
            _timer.Stop();
            AssignHandle(hwd);
        }

        public UsbBarcodeScannerRawInput(Form frm, string endKey, string targetDeviceIds) : this(frm, endKey, new string[] { targetDeviceIds })
        {
        }

        public UsbBarcodeScannerRawInput(Form frm, string endKey, string[] targetDeviceIds)
        {
            hwd = frm.Handle;
            _targetDeviceIds = targetDeviceIds;
            _endKeyStr = endKey;
            _timer.Interval = 20;
            _timer.Tick += (sender, args) => _keys.Clear();
            _timer.Stop();
            AssignHandle(hwd);
        }


        public void Start()
        {
            if (IsCapturing())
                return;

            _isCapturing = true;
            RegisterForRawInput();
            _timer.Start();
        }

        public void Stop()
        {
            if (!IsCapturing())
                return;

            _isCapturing = false;
            _timer.Stop();
            _keys.Clear();
        }

        public bool IsCapturing()
        {
            return _isCapturing;
        }

        #endregion

        #region Private Methods

        private void RegisterForRawInput()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // HID_USAGE_PAGE_GENERIC
            rid[0].usUsage = 0x06; // HID_USAGE_GENERIC_KEYBOARD
            rid[0].dwFlags = 0x00000000; // RIDEV_INPUTSINK
            rid[0].hwndTarget = hwd; // Handle to the window that will receive raw input

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new ApplicationException("Failed to register raw input device(s).");
            }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                uint dwSize = 0;
                GetRawInputData(m.LParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));

                IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
                try
                {
                    if (GetRawInputData(m.LParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) == dwSize)
                    {
                        RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
                        if (raw.header.dwType == RIM_TYPEKEYBOARD)
                        {
                            // Process raw keyboard input
                            RAWINPUTHEADER header = raw.header;
                            ushort key = raw.keyboard.VKey;
                            uint message = raw.keyboard.Message;

                            // Use deviceHandle to determine the source device
                            // Use virtualKey to determine the key pressed

                            // Check if the device handle matches the target device
                            if (IsTargetDevice(header))
                            {
                                // Detect if the input is WM_KEYUP or WM_KEYDOWN
                                if (message == WM_KEYDOWN)
                                {
                                    // Handle key down event
                                    ProcessKeyEvent(key, true);
                                }
                                else if (message == WM_KEYUP)
                                {
                                    // Handle key up event
                                    ProcessKeyEvent(key, false);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }


            base.WndProc(ref m);
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


        private void ProcessKeyEvent(ushort virtualKey, bool isKeyDown)
        {
            // Convert the virtual key code to a Keys enum value
            var key = (Keys)virtualKey;

            _timer.Stop();
            bool shouldRaiseEvent = EndKeyDetected(key);
            if (isKeyDown)
            {
                _keys.Add(key);
                _timer.Start();
            }

            if (shouldRaiseEvent)
            {
                string barcode = GetBarcodeString();
                BarcodeScanned?.Invoke(this, new BarcodeScannedEventArgs(barcode));
                _keys.Clear();
            }
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

            if (key == _endKey || key == Keys.Enter || (!string.IsNullOrEmpty(_endKeyStr) && string.Equals(keyStr, _endKeyStr)))
            {
                return true;
            }

            return false;
        }

        private bool IsTargetDevice(RAWINPUTHEADER header)
        {
            uint pcbSize = 0;

            // First call to get the size of the device name
            if (GetRawInputDeviceInfo(header.hDevice, RIDI_DEVICENAME, IntPtr.Zero, ref pcbSize) != 0)
            {
                // If the function does not return 0, there is an error
                return false;
            }

            if (pcbSize == 0)
            {
                return false;
            }

            // Allocate a buffer to hold the device name
            IntPtr pData = Marshal.AllocHGlobal((int)pcbSize);
            try
            {
                GetRawInputDeviceInfo(header.hDevice, RIDI_DEVICENAME, pData, ref pcbSize);

                string deviceName = Marshal.PtrToStringAnsi(pData);

                // Normalize the device name for comparison
                string normalizedDeviceName = deviceName.Replace("\\", "#");

                // Compare the device name with the target device ID
                foreach (var deviceId in _targetDeviceIds)
                {
                    if (normalizedDeviceName.Contains(deviceId))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pData);
            }

            return false;
        }


        #endregion

        #region Destructor

        ~UsbBarcodeScannerRawInput()
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



        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWKEYBOARD keyboard;
        }


        [StructLayout(LayoutKind.Sequential)]
        struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }


        #endregion
    }
}
