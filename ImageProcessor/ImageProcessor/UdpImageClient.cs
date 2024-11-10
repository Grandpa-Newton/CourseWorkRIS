using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Drawing.Imaging;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace ImageProcessor
{
    public class UdpImageClient
    {
        public async Task<BitmapImage?> GetProcessedImage(Image sourceImage, string ipAddress, Socket? udpSocket, Logger logger, Action? finalCallback = null)
        {
            byte[] data = ImageToByteArray(sourceImage);
            EndPoint endPoint;

            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), 8888);
            }
            catch (Exception ex)
            {
                logger.Log(string.Format("ОШИБКА! {0}", ex.Message));
                udpSocket.Close();
                udpSocket.Dispose();
                return null;
            }
            try
            {
                udpSocket.Connect(endPoint);
            }
            catch (Exception ex)
            {
                logger.Log(string.Format("ОШИБКА! {0}", ex.Message));
                udpSocket.Close();
                udpSocket.Dispose();
                return null;
            }

            int fragmentSize = 60000;
            int fragmentsNumber = (data.Length + fragmentSize - 1) / fragmentSize;

            logger.Log("Началась отправка изображения.");

            for (int i = 0; i < fragmentsNumber; i++)
            {
                int size = Math.Min(fragmentSize, data.Length - i * fragmentSize);

                byte[] fragmentData = new byte[size + 8];
                Array.Copy(data, i * fragmentSize, fragmentData, 8, size);

                byte[] fragmentNumber = BitConverter.GetBytes(i);
                Array.Copy(fragmentNumber, 0, fragmentData, 0, 4);

                byte[] fragmentsNumberBytes = BitConverter.GetBytes(fragmentsNumber);
                Array.Copy(fragmentsNumberBytes, 0, fragmentData, 4, 4);

                try
                {
                    udpSocket.Send(fragmentData);
                }
                catch (Exception ex)
                {
                    logger.Log(string.Format("ОШИБКА! {0}", ex.Message));
                    udpSocket.Close();
                    udpSocket.Dispose();
                    return null;
                }

                logger.Log(string.Format("Отправлено {0} из {1} фрагментов изображения.", i + 1, fragmentsNumber));

                await Task.Delay(50);
            }

            var processedImage = ReceiveImage(udpSocket, logger);

            if(finalCallback != null)
            {
                finalCallback.Invoke();
            }

            return processedImage;
        }

        private BitmapImage? ReceiveImage(Socket udpSocket, Logger logger)
        {
            byte[] buffer = new byte[65535];
            using MemoryStream ms = new MemoryStream();

            while (true)
            {
                int receivedBytes = 0;
                try
                {
                    receivedBytes = udpSocket.Receive(buffer);
                }
                catch (Exception ex)
                {
                    logger.Log(string.Format("ОШИБКА! {0}", ex.Message));
                    udpSocket.Close();
                    udpSocket.Dispose();
                    return null;
                }
                if (receivedBytes > 0)
                {
                    int fragmentNumber = BitConverter.ToInt32(buffer, 0);
                    int fragmentsNumber = BitConverter.ToInt32(buffer, 4);
                    logger.Log(string.Format("Получен фрагмент [{0}/{1}] обработанного изображения.", fragmentNumber + 1, fragmentsNumber));
                    ms.Write(buffer, 8, receivedBytes - 8);
                    if (fragmentNumber + 1 == fragmentsNumber)
                    {
                        break;
                    }
                }
            }

            udpSocket.Close();
            udpSocket.Dispose();

            ms.Position = 0;
            Image processedImage = Image.FromStream(ms);
            processedImage.Save(ms, ImageFormat.Jpeg);

            ms.Position = 0;
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = ms;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            return bitmapImage;
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
