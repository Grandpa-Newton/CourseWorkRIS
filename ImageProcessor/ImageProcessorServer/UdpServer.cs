using System.Collections.Concurrent;
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

    public void StartServer()
    {
        _server.Bind(new IPEndPoint(IPAddress.Any, Port));
        var ipAddress = GetLocalIPAddress();
        Console.WriteLine($"UDP-сервер запущен, адрес: {ipAddress}:{Port}");

        while (true)
        {
            byte[] buffer = new byte[65535];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            int receivedBytes = _server.ReceiveFrom(buffer, ref remoteEndPoint);
            
            ThreadPool.QueueUserWorkItem(_ => ProcessImageData(buffer, receivedBytes, remoteEndPoint));
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

    private ConcurrentDictionary<EndPoint, List<byte[]>> receivedFragments = new ConcurrentDictionary<EndPoint, List<byte[]>>();
    private ConcurrentDictionary<EndPoint, int> fragmentsToExpect = new ConcurrentDictionary<EndPoint, int>();

    private void ProcessImageData(byte[] buffer, int receivedBytes, EndPoint remoteEndPoint)
    {
        int fragmentNumber = BitConverter.ToInt32(buffer, 0);
        int fragmentsNumber = BitConverter.ToInt32(buffer, 4);
        byte[] fragmentData = new byte[receivedBytes - 8];
        Array.Copy(buffer, 8, fragmentData, 0, receivedBytes-8);

        if (!receivedFragments.ContainsKey(remoteEndPoint))
        {
            receivedFragments[remoteEndPoint] = new List<byte[]>();
            fragmentsToExpect[remoteEndPoint] = fragmentsNumber;
        }

        receivedFragments[remoteEndPoint].Add(fragmentData);
        Console.WriteLine($"Получена часть {fragmentNumber+1} из {fragmentsNumber} от {remoteEndPoint}");
        Console.WriteLine($"Количество фрагментов получено: {receivedFragments[remoteEndPoint].Count}");

        if ((receivedFragments[remoteEndPoint].Count == fragmentsToExpect[remoteEndPoint]))
        {
            byte[] fullImageData = CombineFragments(receivedFragments[remoteEndPoint]);
            ProcessFullImage(fullImageData, receivedBytes - 8, remoteEndPoint);
            receivedFragments.Remove(remoteEndPoint, out _);
            fragmentsToExpect.Remove(remoteEndPoint, out _);
        }
    }

    private async void ProcessFullImage(byte[] buffer, int receivedBytes, EndPoint remoteEndPoint)
    {
        using (MemoryStream ms = new MemoryStream(buffer))
        {
            var image = Image.FromStream(ms);

            Image processedImage = ApplyLowPassFilter(image);

            using (MemoryStream outputMs = new MemoryStream())
            {
                processedImage.Save(outputMs, ImageFormat.Jpeg);
                byte[] responseBytes = outputMs.ToArray();

                int fragmentSize = 60000;
                int fragmentsNumber = (responseBytes.Length + fragmentSize - 1) / fragmentSize;

                for (int i = 0; i < fragmentsNumber; i++)
                {
                    int size = Math.Min(fragmentSize, responseBytes.Length - i * fragmentSize);

                    byte[] fragmentData = new byte[size + 8];
                    Array.Copy(responseBytes, i*fragmentSize, fragmentData, 8, size);

                    byte[] fragmentNumber = BitConverter.GetBytes(i);
                    Array.Copy(fragmentNumber, 0, fragmentData, 0, 4);

                    byte[] fragmentsNumberBytes = BitConverter.GetBytes(fragmentsNumber);
                    Array.Copy(fragmentsNumberBytes, 0, fragmentData, 4, 4);
                    
                    _server.SendTo(fragmentData, remoteEndPoint);

                    await Task.Delay(50);
                }
                
                //_server.SendTo(responseBytes, remoteEndPoint);
                Console.WriteLine("Изображение обработано и отправлено обратно клиенту.");
            }
        }
    }

    private byte[] CombineFragments(List<byte[]> fragments)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            foreach (var fragment in fragments)
            {
                ms.Write(fragment, 0, fragment.Length);
            }

            return ms.ToArray();
        }
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