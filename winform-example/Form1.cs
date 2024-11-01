using TownSuite.BarcodeScanner;

namespace winform_example
{
    public partial class Form1 : Form
    {
        //UsbBarcodeScanner scannerListener;
        UsbBarcodeScannerRawInput scannerListener;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //scannerListener = new UsbBarcodeScanner("#", "HID#VID_05E0&PID_1200");
            scannerListener = new UsbBarcodeScannerRawInput(this, "#", "HID#VID_05E0&PID_1200");

            scannerListener.BarcodeScanned += (s, args) =>
            {
                listBox1.Items.Add(args.Barcode);
                textBox1.Focus();
            };

            scannerListener.Start();
        }
    }
}
