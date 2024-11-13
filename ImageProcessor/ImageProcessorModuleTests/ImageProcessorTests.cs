using ImageProcessor;
using ImageProcessorServer;
using System.Drawing;
using System.Net.Sockets;

namespace ImageProcessorModuleTests
{
    [TestClass]
    public class ImageProcessorTests
    {
        private const string IpAddress = "26.41.29.58";
        private static Socket Socket;
        private static int NumberOfPassedTests = 0;
        static async Task fff(string[] args)
        {
            var sourceImage = Image.FromFile("sourceImage.jpg");
            UdpImageClient client = new UdpImageClient();
            var processedImage = await client.GetProcessedImage(sourceImage, IpAddress, Socket, new Logger((msg) =>
            {
                return;
            }));


            await Task.Delay(10);
        }

        [TestMethod]
        public void TestImageProcessing()
        {
            LowPassFilterApplyer lowPassFilterApplyer = new LowPassFilterApplyer();

            var resultImage = lowPassFilterApplyer.ApplyLowPassFilter(Image.FromFile("sourceImage.jpg"), 6);

            var trueImage = Image.FromFile("resultImage.jpg");

            var resultImageBitmap = new Bitmap(resultImage);
            var trueImageBitmap = new Bitmap(trueImage);

            if (resultImageBitmap.Size == trueImageBitmap.Size)
            {
                for (var i = 0; i < resultImageBitmap.Height; i++)
                {
                    for (var j = 0; j < resultImageBitmap.Width; j++)
                    {
                        var resultImagePixel = resultImageBitmap.GetPixel(j, i);
                        var trueImagePixel = trueImageBitmap.GetPixel(j, i);
                        Assert.IsTrue(resultImagePixel == trueImagePixel);
                    }
                }
            }
        }
    }
}