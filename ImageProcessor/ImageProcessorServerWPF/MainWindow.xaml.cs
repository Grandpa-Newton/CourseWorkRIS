using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;

namespace ImageProcessorServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const int Port = 8080;
        UdpClient _udpServer;
        private TcpListener _listener;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void GetMessage()
        {
            while (true)
            {
                var result = await _listener.AcceptTcpClientAsync();

                byte[] buffer = new byte[result.ReceiveBufferSize];
                var bytes = await result.GetStream().ReadAsync(buffer, 0, buffer.Length);

                using (var ms = new MemoryStream(buffer))
                {
                    var image = Image.FromStream(ms);
                    image.Save(ms, ImageFormat.Png);
                    ms.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = ms;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    ShowImage(bitmapImage);
                }

                InfoLabel.Content = $"Получено {bytes} байт\n\r" +
                    $"Удаленный адрес: {result.Client.RemoteEndPoint}\r\n";
            }
        }

        private void ShowImage(BitmapImage image)
        {
            sendingImage.Source = image;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            
            /*_udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
*/
            var ipAddress = GetLocalIPAddress();
            InfoLabel.Content = $"Сервер {ipAddress}:{Port} запущен.";

            
            
            GetMessage();
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Локальный IP-адрес не найден.");
        }
    }
}