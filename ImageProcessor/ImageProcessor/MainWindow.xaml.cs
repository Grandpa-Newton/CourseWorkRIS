using Microsoft.Win32;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Socket _udpSocket;
        private Image _currentImage;
        public MainWindow()
        {
            InitializeComponent();
            
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        }

        private void SendMessage()
        {
            if(_currentImage == null)
            {
                return;
            }

            byte[] data = ImageToByteArray(_currentImage);
            EndPoint endPoint = new IPEndPoint(IPAddress.Parse(IpTextBox.Text), 8080);

            int fragmentSize = 60000;
            int fragmentsNumber = (data.Length + fragmentSize - 1) / fragmentSize;

            for (int i = 0; i < fragmentsNumber; i++)
            {
                int size = Math.Min(fragmentSize, data.Length - i * fragmentSize);

                byte[] fragmentData = new byte[size + 8];
                Array.Copy(data, i*fragmentSize, fragmentData, 8, size);

                byte[] fragmentNumber = BitConverter.GetBytes(i);
                Array.Copy(fragmentNumber, 0, fragmentData, 0, 4);

                byte[] fragmentsNumberBytes = BitConverter.GetBytes(fragmentsNumber);
                Array.Copy(fragmentsNumberBytes, 0, fragmentData, 4, 4);
                
                _udpSocket.SendTo(fragmentData, endPoint);
                
                Task.Delay(500);
            }
            
            //_udpSocket.SendTo(data, endPoint);

            ReceiveImage(endPoint);
        }

        private void ReceiveImage(EndPoint endPoint)
        {
            byte[] buffer = new byte[65535];
            using MemoryStream ms = new MemoryStream();

            while (true)
            {
                int receivedBytes = _udpSocket.ReceiveFrom(buffer, ref endPoint);
                if (receivedBytes > 0)
                {
                    Console.WriteLine("Получил фрагмент.");
                    ms.Write(buffer, 0, receivedBytes);
                    if (receivedBytes < buffer.Length)
                    {
                        break;
                    }
                }
            }

            ms.Position = 0;
            Image processedImage = Image.FromStream(ms);
            processedImage.Save(ms, ImageFormat.Jpeg);
            
            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            ShowImage(bitmapImage);
        }

        private void ShowImage(BitmapImage bitmapImage)
        {
            ImageToSend.Source = bitmapImage;
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