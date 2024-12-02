using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessorServer;

public class LowPassFilterApplyer
{
    private readonly double[,] _kernel = new double[3, 3]
    {
        {1f, 1f, 1f},
        {1f, 1f, 1f},
        {1f, 1f, 1f}
    };

    private const float defaultMultiplier = 1 / 9f;

    private int _threadsFinished;
    private int _bitmapWidth;
    private int _bitmapHeight;

    public Image ApplyLowPassFilter(Image inputImage, float brightnessFactor)
    {
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

        var buffer = new byte[data.Stride * data.Height];

        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

        Calculate(0, _bitmapWidth, buffer, depth, data.Stride, brightnessFactor);

        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

        bitmap.UnlockBits(data);

        return bitmap;
    }

    public void Calculate(int startIndex, int finishIndex,
        byte[] buffer, int depth, int stride, float brightnessFactor)
    {
        var brightnessMultiplier = defaultMultiplier * brightnessFactor;

        var offset = _kernel.GetLength(0) / 2;

        for (int i = startIndex; i < finishIndex; i++)
        {
            for (int j = 0; j < _bitmapHeight; j++)
            {
                double r = 0;
                double g = 0;
                double b = 0;

                for (int yFilter = 0; yFilter < _kernel.GetLength(0); yFilter++)
                {
                    int pk = (yFilter + i - offset < 0) ? 0 :
                        (yFilter + i - offset >= _bitmapWidth) ? _bitmapWidth - 1 :
                        yFilter + i - offset;
                    for (int xFilter = 0; xFilter < _kernel.GetLength(0); xFilter++)
                    {
                        int pl = (xFilter + j - offset < 0) ? 0 :
                            (xFilter + j - offset >= _bitmapHeight) ? _bitmapHeight - 1 :
                            xFilter + j - offset;

                        int byteColorOffset = pl * stride + pk * depth;

                        b += buffer[byteColorOffset] * _kernel[yFilter, xFilter] * brightnessMultiplier;
                        g += buffer[byteColorOffset + 1] * _kernel[yFilter, xFilter] * brightnessMultiplier;
                        r += buffer[byteColorOffset + 2] * _kernel[yFilter, xFilter] * brightnessMultiplier;
                    }
                }

                int byteOffset = j * stride + i * depth;

                buffer[byteOffset] = (byte)Normalize(b);
                buffer[byteOffset + 1] = (byte)Normalize(g);
                buffer[byteOffset + 2] = (byte)Normalize(r);
            }
        }
    }

    public Image ApplyLowPassFilter(Image inputImage, int threadsCount, float brightnessFactor)
    {
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

        var buffer = new byte[data.Stride * data.Height];
        
        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
        
        for (int i = 0, start = 0; i < threadsCount; i++)
        {
            var size = (bitmap.Width - i + threadsCount - 1) / threadsCount;
            var it = start;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Calculate(it, it + size, buffer, depth, data.Stride, brightnessFactor);
                _threadsFinished++;
            });

            start += size;
        }

        while (_threadsFinished < threadsCount)
        {
            // Wait
        }

        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);

        bitmap.UnlockBits(data);

        return bitmap;
    }

    private int Normalize(double value)
    {
        return Math.Min(Math.Max((int)value, 0), 255);
    }
}