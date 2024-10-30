using System;

namespace BasselTech.UsbBarcodeScanner
{
    public interface IUsbBarcodeScanner
    {
        event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        bool IsCapturing();
        void Start();
        void Stop();
    }
}