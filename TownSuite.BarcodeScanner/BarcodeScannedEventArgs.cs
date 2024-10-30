using System;

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
    }
}
