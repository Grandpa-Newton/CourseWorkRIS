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

            _udpSocket.Connect(new IPEndPoint(IPAddress.Parse(IpTextBox.Text), 8080));

            _udpSocket.Send(data);

            ReceiveImage();
        }

        private void ReceiveImage()
        {
            byte[] buffer = new byte[65535];
            
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int receivedBytes = _udpSocket.ReceiveFrom(buffer, ref remoteEndPoint);

            using (MemoryStream ms = new MemoryStream(buffer, 0, receivedBytes))
            {
                Image processedImage = Image.FromStream(ms);
                // Сохраните или отобразите обработанное изображение
                //processedImage.Save("processed_image.jpg");
                //MessageBox.Show("Обработанное изображение получено и сохранено как processed_image.jpg");
                processedImage.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                ShowImage(bitmapImage);
            }
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