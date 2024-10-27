using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

namespace ImageProcessorServer;

public class UdpServer
{
    private const int Port = 8080;
    private Socket _server;

    public UdpServer()
    {
        _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public async Task StartServer()
    {
        _server.Bind(new IPEndPoint(IPAddress.Any, Port));
        var ipAddress = GetLocalIPAddress();
        Console.WriteLine($"UDP-сервер запущен, адрес: {ipAddress}:{Port}");

        while (true)
        {
            byte[] buffer = new byte[65535];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int receivedBytes = _server.ReceiveFrom(buffer, ref remoteEndPoint);
            
            ThreadPool.QueueUserWorkItem(_ => ProcessImage(buffer, receivedBytes, remoteEndPoint));
        }
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

    private void ProcessImage(byte[] buffer, int receivedBytes, EndPoint remoteEndPoint)
    {
        using (MemoryStream ms = new MemoryStream(buffer, 0 , receivedBytes))
        {
            var image = Image.FromStream(ms);

            Image processedImage = ApplyLowPassFilter(ChangeBrightness(image, 1.2f));

            using (MemoryStream outputMs = new MemoryStream())
            {
                processedImage.Save(outputMs, ImageFormat.Jpeg);
                byte[] responseBytes = outputMs.ToArray();

                // Отправляем обработанное изображение обратно клиенту
                _server.SendTo(responseBytes, remoteEndPoint);
                Console.WriteLine("Изображение обработано и отправлено обратно клиенту.");
            }
        }
    }

    static Image ChangeBrightness(Image image, float brightnessFactor)
    {
        Bitmap tempBitmap = new Bitmap(image.Width, image.Height);
        Graphics g = Graphics.FromImage(tempBitmap);
        float[][] ptsArray =
        {
            new float[] { brightnessFactor, 0, 0, 0, 0 },
            new float[] { 0, brightnessFactor, 0, 0, 0 },
            new float[] { 0, 0, brightnessFactor, 0, 0 },
            new float[] { 0, 0, 0, 1, 0 },
            new float[] { 0, 0, 0, 0, 1 }
        };
        ImageAttributes attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
        g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
            0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
        g.Dispose();
        return tempBitmap;
    }

    static Image ApplyLowPassFilter(Image image)
    {
        Bitmap bitmap = new Bitmap(image);
        for (int x = 1; x < bitmap.Width - 1; x++)
        {
            for (int y = 1; y < bitmap.Height - 1; y++)
            {
                Color prevX = bitmap.GetPixel(x - 1, y);
                Color nextX = bitmap.GetPixel(x + 1, y);
                Color prevY = bitmap.GetPixel(x, y - 1);
                Color nextY = bitmap.GetPixel(x, y + 1);
                int avgR = (prevX.R + nextX.R + prevY.R + nextY.R) / 4;
                int avgG = (prevX.G + nextX.G + prevY.G + nextY.G) / 4;
                int avgB = (prevX.B + nextX.B + prevY.B + nextY.B) / 4;
                Color avgColor = Color.FromArgb(avgR, avgG, avgB);
                bitmap.SetPixel(x, y, avgColor);
            }
        }

        return bitmap;
    }
}