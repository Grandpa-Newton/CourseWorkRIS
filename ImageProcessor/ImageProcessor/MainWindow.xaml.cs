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
        private Logger _logger;
        private UdpImageClient _udpClient = new();
        public MainWindow()
        {
            InitializeComponent();

            this.WindowState = WindowState.Maximized;

            _logger = new Logger((message) =>
            {
                Console.WriteLine(message);
                infoLabel.Content = message;
            });
        }

        private async void SendMessage()
        {
            if(_currentImage == null)
            {
                _logger.Log("Для начала выберите изображение.");
                return;
            }

            var processedImage = await _udpClient.GetProcessedImage(_currentImage, IpTextBox.Text, _udpSocket, _logger);

            if(processedImage == null)
            {
                return;
            }

            ShowImage(processedImage);
        }

        private void ShowImage(BitmapImage bitmapImage)
        {
            ProcessedImage.Source = bitmapImage;
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.DefaultExt = ".jpg";
            fileDialog.Filter = "Jpeg Images (.jpg)|*.png";

            var showDialogResult = fileDialog.ShowDialog();

            if (showDialogResult.HasValue && showDialogResult.Value)
            {
                StreamReader sr = new StreamReader(fileDialog.FileName);
                _currentImage = Image.FromFile(fileDialog.FileName);
                using MemoryStream ms = new MemoryStream();
                ms.Position = 0;
                _currentImage.Save(ms, ImageFormat.Jpeg);
            
                ms.Position = 0;
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                OpenedImage.Source = bitmapImage;
            }
        }
    }
}