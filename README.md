Fork of https://github.com/BasselTech/usb-barcode-scanner-lib.git

# USB Barcode Scanner Library

## Description
The **USB Barcode Scanner Library** is a .NET framework library originally developed by **BasselTech** and this fork by TownSuite in C#. It allows capturing the barcodes scanned by USB barcode scanners in both C# and VB .NET applications.

## Features
- Capture barcodes scanned by USB barcode scanners, even when the application window is out of focus.
- Easily integrate barcode scanning functionality into C# and VB .NET applications.

## Build from Source
To build the **USB Barcode Scanner Library** from source, follow these steps:

1. Clone the repository:
   ```bash
   git clone https://github.com/TownSuite/usb-barcode-scanner-lib.git
   ```

2. Navigate to the project directory:
   ```bash
   cd usb-barcode-scanner-lib
   ```

3. Build the project using Visual Studio or the .NET CLI:
   - Using Visual Studio:
     - Open the solution file `usb-barcode-scanner-lib.sln` in Visual Studio.
     - Build the solution (Ctrl+Shift+B).

   - Using .NET CLI:
     ```bash
     dotnet build
     ```

4. Once built successfully, the library files will be available in the `bin` directory.


## Installation & Usage
To use the **USB Barcode Scanner Library** in your C# or VB .NET application, follow these steps:

1. Download the latest release from the [Releases](https://github.com/TownSuite/usb-barcode-scanner-lib/releases) page.

2. Extract the downloaded files into your project directory.

3. Add a reference to the library file `USB-Barcode-Scanner.dll` in your C# or VB .NET project.

4. Initialize a USB barcode scanner object.

5. Subscribe to the `BarcodeScanned` event to handle scanned barcode data.

6. Start capturing barcode scans.

### UsbBarcodeScanner

#### Example usage in C#:

```csharp
using TownSuite.BarcodeScanner;

// Initialize USB barcode scanner
var scanner = new UsbBarcodeScanner();

// Subscribe to BarcodeScanned event
scanner.BarcodeScanned += (sender, args) =>
{
    Console.WriteLine($"Scanned barcode: {args.Barcode}");
};

// Start capturing barcode scans
scanner.Start();
```

#### Example usage in VB .NET:

```vb
Imports TownSuite.BarcodeScanner

' Initialize USB barcode scanner
Dim scanner As New UsbBarcodeScanner()

' Subscribe to BarcodeScanned event
AddHandler scanner.BarcodeScanned, Sub(sender, args)
                                       Console.WriteLine($"Scanned barcode: {args.Barcode}")
                                   End Sub

' Start capturing barcode scans
scanner.Start()
```

### UsbBarcodeScannerRawInput

Use the UsbBarcodeScannerRawInput scanner when input from a particular device and only that device is desired.

#### Example usage in C#:
```csharp
using TownSuite.BarcodeScanner;

// Initialize USB barcode scanner
var scanner = new UsbBarcodeScannerRawInput(formInstance, "#", "HID#VID_05E0&PID_1200");

// Subscribe to BarcodeScanned event
scanner.BarcodeScanned += (sender, args) =>
{
    Console.WriteLine($"Scanned barcode: {args.Barcode}");
};

// Start capturing barcode scans
scanner.Start();
```

####Example usage in VB .NET:

```vb
Imports TownSuite.BarcodeScanner

' Initialize USB barcode scanner
Dim scanner As New UsbBarcodeScannerRawInput(formInstance, "#", "HID#VID_05E0&PID_1200");

' Subscribe to BarcodeScanned event
AddHandler scanner.BarcodeScanned, Sub(sender, args)
                                       Console.WriteLine($"Scanned barcode: {args.Barcode}")
                                   End Sub

' Start capturing barcode scans
scanner.Start()
```

## Contributing
We welcome contributions from the community! If you'd like to contribute to the **USB Barcode Scanner Library**, please follow these guidelines:

1. Fork the repository.
2. Create a new branch: `git checkout -b feature/new-feature`.
3. Make your changes and commit them: `git commit -am 'Add new feature'`.
4. Push to the branch: `git push origin feature/new-feature`.
5. Submit a pull request.

## Bug Reporting
If you encounter any bugs or issues with the **USB Barcode Scanner Library**, please open an issue on the repository. Include a detailed description of the problem and any relevant information for reproducing it.

## License
This project is licensed under the Apache License 2.0 - see the [LICENSE](https://github.com/TownSuite/usb-barcode-scanner-lib?tab=Apache-2.0-1-ov-file#readme) file for details.