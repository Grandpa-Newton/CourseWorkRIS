using System.Drawing;

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
    private List<Thread> _threads;
    private int _bitmapWidth;
    private int _bitmapHeight;

    public Image ApplyLowPassFilter(Image inputImage)
    {
        Console.WriteLine("Началась обработка изображения");
        
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;
        Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

        Calculate(0, bitmap.Width, bitmap, result);

        return result;
    }

    private void Calculate(int startIndex, int finishIndex, 
        Bitmap bitmap, Bitmap result)
    {
        var offset = _kernel.GetLength(0) / 2;
        for (int i = startIndex; i < finishIndex; i++)
        {
            for (int j = 0; j < _bitmapHeight; j++)
            {
                var colorMap = new Color[_kernel.GetLength(0), _kernel.GetLength(0)];

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

                        lock (bitmap)
                        {
                            colorMap[yFilter, xFilter] = bitmap.GetPixel(pk, pl);
                        }
                    }
                }

                Color resultColor = MultiplyColorWithKernel(colorMap);

                lock (result)
                {
                    result.SetPixel(i, j, resultColor);
                }
            }
        }
    }

    public Image ApplyLowPassFilter(Image inputImage, int threadsCount)
    {
        _threads = new List<Thread>();
        Bitmap bitmap = new Bitmap(inputImage);
        _bitmapWidth = bitmap.Width;
        _bitmapHeight = bitmap.Height;
        Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

        for (int i = 0, start = 0; i < threadsCount; i++)
        {
            var size = (bitmap.Width - i + threadsCount - 1) / threadsCount;
            var it = start;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                Calculate(it, it + size, bitmap, result);
                _threadsFinished++;
            });

            start += size;
        }

        while (_threadsFinished < threadsCount)
        {
            // Wait
        }

        return result;
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