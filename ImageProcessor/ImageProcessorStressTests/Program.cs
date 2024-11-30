using ImageProcessor;
using System.Drawing;
using System.Net.Sockets;

namespace ImageProcessorStressTests
{
    internal class Program
    {
        private const int NumberOfTests = 10000;
        private const string IpAddress = "26.41.29.58";
        private static Socket Socket;
        private static int NumberOfPassedTests = 0;
        static async Task Main(string[] args)
        {
            var testImage = Image.FromFile("tstImage.jpg");

            for (int i = 0; i < NumberOfTests; i++)
            {
                int numberOfTest = i;
                UdpImageClient client = new UdpImageClient();
                Console.WriteLine($"Запущена обработка {i}-го изображения");
                client.GetProcessedImage(testImage, IpAddress, Socket, new Logger((msg) =>
                {
                    return;
                }), () => 
                {
                    Console.WriteLine($"Обработано {numberOfTest}-е изображение.");
                    NumberOfPassedTests++;
                });

                await Task.Delay(10);
            }

            while(NumberOfPassedTests != NumberOfTests)
            {
                // Wait
            }

            Console.WriteLine("Все тесты пройдены!");
        }
    }
}
