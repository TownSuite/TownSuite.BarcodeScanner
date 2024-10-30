using System;

namespace TownSuite.BarcodeScanner
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
