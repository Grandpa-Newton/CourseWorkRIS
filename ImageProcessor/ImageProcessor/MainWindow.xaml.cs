using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Drawing;
using System.IO;

namespace ImageProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket _udpSocket;
        Image _currentImage;
        public MainWindow()
        {
            InitializeComponent();
            
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        private void SendMessage()
        {
            if(_currentImage == null)
            {
                return;
            }

            byte[] data = ImageToByteArray(_currentImage);

            _udpSocket.Connect(new IPEndPoint(IPAddress.Parse(IpTextBox.Text), 8080));

            _udpSocket.Send(data);
            
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".jpg";
            fileDialog.Filter = "Jpeg Images (.jpg)|*.jpg";

            var showDialogResult = fileDialog.ShowDialog();

            if (showDialogResult.HasValue && showDialogResult.Value)
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(fileDialog.FileName);
                _currentImage = Image.FromFile(fileDialog.FileName);
            }
        }

        private byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }
    }
}