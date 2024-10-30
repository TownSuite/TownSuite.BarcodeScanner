using System;

namespace TownSuite.BarcodeScanner
{
    public interface IUsbBarcodeScanner
    {
        event EventHandler<BarcodeScannedEventArgs> BarcodeScanned;

        bool IsCapturing();
        void Start();
        void Stop();
    }
}