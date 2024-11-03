using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageProcessorServer;

public class LowPassFilterApplyer
{
    private readonly double[,] _kernel = new double[3,3]
    {
        {1, 1, 1},
        {1, 1, 1},
        {1, 1, 1}
    };

    private int _threadsFinished;
    private int _bitmapWidth;
    private int _bitmapHeight;

    public Image ApplyLowPassFilter(Image inputImage)
    {
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

        var buffer = new byte[data.Stride * data.Height];
        
        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);
        
        Calculate(0, _bitmapWidth, buffer, depth, data.Stride);
        
        Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
        
        bitmap.UnlockBits(data);

        return bitmap;
    }

    private void Calculate(int startIndex, int finishIndex, 
        byte[] buffer, int depth, int stride)
    {
        var offset = _kernel.GetLength(0) / 2;
        
        for (int i = startIndex; i < finishIndex; i++)
        {
            for (int j = 0; j < _bitmapHeight; j++)
            {
                var colorMap = new Color[_kernel.GetLength(0), _kernel.GetLength(0)];

                int byteOffset = j * stride + i * depth;
                
                for (int yFilter = 0; yFilter < _kernel.GetLength(0); yFilter++)
                {
                    int pk = (yFilter + i - offset <= 0) ? 0 :
                        (yFilter + i - offset >= _bitmapWidth - 1) ? _bitmapWidth - 1 :
                        yFilter + i - offset;
                    for (int xFilter = 0; xFilter < _kernel.GetLength(0); xFilter++)
                    {
                        int pl = (xFilter + j - offset <= 0) ? 0 :
                            (xFilter + j - offset >= _bitmapHeight - 1) ? _bitmapHeight - 1 :
                            xFilter + j - offset;

                        var byteColorOffset = pl * stride + pk * depth;

                        colorMap[yFilter, xFilter] = Color.FromArgb(buffer[byteColorOffset], 
                            buffer[byteColorOffset + 1], buffer[byteColorOffset + 2]);
                    }
                }

                Color resultColor = MultiplyColorWithKernel(colorMap);

                buffer[byteOffset] = resultColor.R;
                buffer[byteOffset + 1] = resultColor.G;
                buffer[byteOffset + 2] = resultColor.B;
            }
        }
    }

    public Image ApplyLowPassFilter(Image inputImage, int threadsCount)
    {
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;

        var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
        var depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

        var buffer = new byte[data.Stride * data.Height];
        
        Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

        /*Parallel.For(0, threadsCount, threadNumber =>
            {
                var size = (bitmap.Width - threadNumber + threadsCount - 1) / threadsCount;
                int it = 0;

                for (int i = 0; i < threadNumber; i++)
                {
                    it += (bitmap.Width - i + threadsCount - 1) / threadsCount;
                }
                
                Calculate(it, it + size, buffer, depth, data.Stride);
            });*/
        
        for (int i = 0, start = 0; i < threadsCount; i++)
        {
            var size = (bitmap.Width - i + threadsCount - 1) / threadsCount;
            var it = start;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Calculate(it, it + size, buffer, depth, data.Stride);
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

    private Color MultiplyColorWithKernel(Color[,] colorMap)
    {
        double r = 0;
        double g = 0;
        double b = 0;

        for (int i = 0; i < _kernel.GetLength(0); i++)
        {
            for (int j = 0; j < _kernel.GetLength(0); j++)
            {
                r += colorMap[j, i].R * _kernel[j, i];
                g += colorMap[j, i].G * _kernel[j, i];
                b += colorMap[j, i].B * _kernel[j, i];
            }
        }

        return Color.FromArgb(Normalize(r), Normalize(g), Normalize(b));
    }

    private int Normalize(double value)
    {
        return Math.Min(Math.Max((int)(value/9f), 0), 255);
    }
}