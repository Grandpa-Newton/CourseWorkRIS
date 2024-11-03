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
    public Image ApplyLowPassFilter(Image inputImage)
    {
        Console.WriteLine("Началась обработка изображения");
        
        Bitmap bitmap = new Bitmap(inputImage);

        Bitmap result = new Bitmap(bitmap.Width, bitmap.Height);

        var offset = _kernel.GetLength(0) / 2;

        for (int i = 0; i < bitmap.Width; i++)
        {
            for (int j = 0; j < bitmap.Height; j++)
            {
                var colorMap = new Color[_kernel.GetLength(0), _kernel.GetLength(0)];

                for (int yFilter = 0; yFilter < _kernel.GetLength(0); yFilter++)
                {
                    int pk = (yFilter + i - offset <= 0) ? 0 :
                        (yFilter + i - offset >= bitmap.Width - 1) ? bitmap.Width - 1 :
                        yFilter + i - offset;
                    for (int xFilter = 0; xFilter < _kernel.GetLength(0); xFilter++)
                    {
                        int pl = (xFilter + j - offset <= 0) ? 0 :
                            (xFilter + j - offset >= bitmap.Height - 1) ? bitmap.Height - 1 :
                            xFilter + j - offset;

                        colorMap[yFilter, xFilter] = bitmap.GetPixel(pk, pl);
                    }
                }

                Color resultColor = MultiplyColorWithKernel(colorMap);
                
                result.SetPixel(i, j, resultColor);
            }
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