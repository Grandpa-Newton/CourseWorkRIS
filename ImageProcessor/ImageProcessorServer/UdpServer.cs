using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;

namespace ImageProcessorServer;

public class UdpServer
{
    private const int Port = 8888;
    private Socket _server;
    private int _threadsCount = 3;
    private const bool IsMultiThread = true;
    private const int MaxThreads = 14;
    private SemaphoreSlim _semaphore;
    public UdpServer()
    {
        _server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _semaphore = new SemaphoreSlim(IsMultiThread ? (int)(MaxThreads / (float)_threadsCount) : MaxThreads);
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

            ThreadPool.QueueUserWorkItem(async _ =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    await ProcessImageData(buffer, receivedBytes, remoteEndPoint);
                }
                finally
                {
                    _semaphore.Release();
                }
            });
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

    private async Task ProcessImageData(byte[] buffer, int receivedBytes, EndPoint remoteEndPoint)
    {
        int fragmentNumber = BitConverter.ToInt32(buffer, 0);
        int fragmentsNumber = BitConverter.ToInt32(buffer, 4);
        float brigthnessMultiplier = BitConverter.ToSingle(buffer, 8);
        byte[] fragmentData = new byte[receivedBytes - 12];
        Array.Copy(buffer, 12, fragmentData, 0, receivedBytes - 12);

        if (!receivedFragments.ContainsKey(remoteEndPoint))
        {
            receivedFragments[remoteEndPoint] = new List<byte[]>();
            fragmentsToExpect[remoteEndPoint] = fragmentsNumber;
        }

        receivedFragments[remoteEndPoint].Add(fragmentData);
        Console.WriteLine($"Получена часть {fragmentNumber + 1} из {fragmentsNumber} от {remoteEndPoint}");
        Console.WriteLine($"Количество фрагментов получено: {receivedFragments[remoteEndPoint].Count}");

        if ((receivedFragments[remoteEndPoint].Count == fragmentsToExpect[remoteEndPoint]))
        {
            byte[] fullImageData = CombineFragments(receivedFragments[remoteEndPoint]);
            await ProcessFullImage(fullImageData, brigthnessMultiplier, remoteEndPoint);
            receivedFragments.Remove(remoteEndPoint, out _);
            fragmentsToExpect.Remove(remoteEndPoint, out _);
        }
    }

    private async Task ProcessFullImage(byte[] buffer, float brigthnessMultiplier, EndPoint remoteEndPoint)
    {
        using (MemoryStream ms = new MemoryStream(buffer))
        {
            var image = Image.FromStream(ms);

            Image processedImage = ApplyLowPassFilter(image, brigthnessMultiplier);

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
                    Array.Copy(responseBytes, i * fragmentSize, fragmentData, 8, size);

                    byte[] fragmentNumber = BitConverter.GetBytes(i);
                    Array.Copy(fragmentNumber, 0, fragmentData, 0, 4);

                    byte[] fragmentsNumberBytes = BitConverter.GetBytes(fragmentsNumber);
                    Array.Copy(fragmentsNumberBytes, 0, fragmentData, 4, 4);

                    _server.SendTo(fragmentData, remoteEndPoint);

                    await Task.Delay(50);
                }

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

    private Image ApplyLowPassFilter(Image image, float brightnessMultiplier)
    {
        Stopwatch stopwatch = new Stopwatch();

        Image? result;

        if (IsMultiThread)
        {
            var multiThreadApplyer = new LowPassFilterApplyer();

            stopwatch.Restart();

            result = multiThreadApplyer.ApplyLowPassFilter(image, _threadsCount, brightnessMultiplier);
            
            Console.WriteLine($"Время, потраченное на обработку {_threadsCount} потоками: {stopwatch.ElapsedMilliseconds}");
        }
        else
        {
            var filterApplyer = new LowPassFilterApplyer();
            stopwatch.Start();
            result = filterApplyer.ApplyLowPassFilter(image, brightnessMultiplier);
            Console.WriteLine($"Время, потраченное на обработку линейным способом: {stopwatch.ElapsedMilliseconds}");
        }

        return result;
    }
}