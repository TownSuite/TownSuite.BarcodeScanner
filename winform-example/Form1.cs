using BasselTech.UsbBarcodeScanner;

namespace winform_example
{
    public partial class Form1 : Form
    {
        UsbBarcodeScanner scannerListener;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            scannerListener = new UsbBarcodeScanner("#", "HID#VID_05E0&PID_1200");

            scannerListener.BarcodeScanned += (s, args) =>
            {
                listBox1.Items.Add(args.Barcode);
            };

            scannerListener.Start();
        }
    }
}