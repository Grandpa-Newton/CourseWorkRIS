using ImageProcessor;
using ImageProcessorServer;
using System.Drawing;
using System.Net.Sockets;

namespace ImageProcessorModuleTests
{
    [TestClass]
    public class ImageProcessorTests
    {
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