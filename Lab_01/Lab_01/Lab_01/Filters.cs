using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.Security.Policy;
using System.Net.NetworkInformation;

namespace Lab_01
{
    public abstract class Filters
    {
        protected abstract Color calculateNewPixelColor(Bitmap sourceImage, int x, int y);
        public Bitmap processImage(Bitmap sourceImage,BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j));
                }
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending) { return null; }
            }
            return resultImage;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }
    }
    
    public class InvertFilter : Filters 
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(255 - sourceColor.R,
                                                255 - sourceColor.G,
                                                255 - sourceColor.B);
            return resultColor;
        }
    }

    public class GrayScale : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
            Color resultColor = Color.FromArgb((int)intensity,
                                                (int)intensity,
                                                (int)intensity);
            return resultColor;
        }
    }

    public class SepiaFilter : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            double intensity = 0.299 * sourceColor.R + 0.587 * sourceColor.G + 0.114 * sourceColor.B;
            double k = 20;
            Color resultColor = Color.FromArgb(Clamp((int)(intensity + 2 * k), 0, 255),
                                                Clamp((int)(intensity + 0.5 * k), 0, 255),
                                                Clamp((int)(intensity - 1 * k), 0, 255));
            return resultColor;
        }
    }

    public class BrightnessIncrease : Filters
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            int k = 20;
            Color resultColor = Color.FromArgb(Clamp(sourceColor.R + k, 0, 255),
                                                Clamp(sourceColor.G + k, 0, 255),
                                                Clamp(sourceColor.B + k, 0, 255));
            return resultColor;
        }
    }

    class ShiftFilter : Filters //подумать над обрезкой
    {
        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int k = 50;
            Color sourceColor = sourceImage.GetPixel(x, y);
            int sourceX = x - k;
            Color resultColor = Color.Empty;
            if (sourceX < 0 || sourceX >= sourceImage.Width) {
                return resultColor;
            }
            resultColor = sourceImage.GetPixel(sourceX, y);
            return resultColor;
        }
    }



    public class MatrixFilter : Filters 
    {
        protected float[,] kernel = null;
        protected MatrixFilter() { }
        public MatrixFilter(float[,] kernel) { this.kernel = kernel; }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;  
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++) 
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neigbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neigbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neigbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neigbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            return Color.FromArgb(Clamp((int)resultR, 0, 255), Clamp((int)resultG, 0, 255), Clamp((int)resultB, 0, 255));
        }
    }
    class BlurFilter : MatrixFilter //not needed
    {
        public BlurFilter() 
        {
            int sizeX = 3;
            int sizeY = 3;  
            kernel = new float[sizeX , sizeY];
            for (int i= 0; i < sizeX; i++) {
                for(int j= 0; j < sizeY; j++) {
                    kernel[i, j] = 1.0f / (float)(sizeX * sizeY);
                }
            }
        }
    }

    class GaussianFilter : MatrixFilter //not needed
    {
        public GaussianFilter() 
        {
            createGaussianKernel(3, 2);
        }
        public void createGaussianKernel(int radius,float sigma)
        {
            int size = 2 * radius + 1;
            kernel = new float[size, size];
            float norm = 0;
            for (int i = -radius; i <= radius; i++)
            {
                for( int j = -radius; j <= radius; j++)
                {
                    kernel[i + radius, j + radius] = (float)(Math.Exp(-(i * i + j * j) / (2 * sigma * sigma)));
                    norm += kernel[i + radius, j + radius];
                }
            }
            for (int i = 0;  i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] /= norm;
                }
            }
        }
    }

    class StampFilter : MatrixFilter //требует доработок
    {
        public StampFilter()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            kernel[0, 0] = 0.0f;
            kernel[0, 1] = 1.0f;
            kernel[0, 2] = 0.0f;
            kernel[1, 0] = -1.0f;
            kernel[1, 1] = 0.0f;
            kernel[1, 2] = 1.0f;
            kernel[2, 0] = 0.0f;
            kernel[2, 1] = -1.0f;
            kernel[2, 2] = 0.0f;

        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            float resultR = 0;
            float resultG = 0;
            float resultB = 0;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neigbourColor = sourceImage.GetPixel(idX, idY);
                    resultR += neigbourColor.R * kernel[k + radiusX, l + radiusY];
                    resultG += neigbourColor.G * kernel[k + radiusX, l + radiusY];
                    resultB += neigbourColor.B * kernel[k + radiusX, l + radiusY];
                }
            }
            resultR = Clamp((int)resultR, 0, 255);
            resultG = Clamp((int)resultG, 0, 255);
            resultB = Clamp((int)resultB, 0, 255);
            resultR += 100; resultG += 100; resultB += 100;
            double intensity = 0.299 * resultR + 0.587 * resultG + 0.114 * resultB;

            Color resultColor = Color.FromArgb(Clamp((int)intensity, 0, 255),
                                                Clamp((int)intensity, 0, 255),
                                                Clamp((int)intensity, 0, 255));

            return resultColor;

        }

    }


    class MotionBlur : MatrixFilter 
    {
        public MotionBlur()
        {
            int size = 9;
            kernel = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] = 0;
                    if (i == j)
                    {
                        kernel[i, j] = 1.0f / (float)size;
                    }
                }
            }
        }
    }

    class GrayWorld
    {

        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            float Avg = 0, AvgR = 0, AvgG = 0, AvgB = 0;
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color pixelColor = sourceImage.GetPixel(i, j);
                    AvgR += pixelColor.R;
                    AvgG += pixelColor.G;
                    AvgB += pixelColor.B;
                }
            }
            AvgR = AvgR / (float)(sourceImage.Width * sourceImage.Height);
            AvgG = AvgG / (float)(sourceImage.Width * sourceImage.Height);
            AvgB = AvgB / (float)(sourceImage.Width * sourceImage.Height);
            Avg = (AvgR + AvgG + AvgB) / 3.0f;


            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColorAvg(sourceImage, i, j, Avg, AvgR, AvgG, AvgB));
                }
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending) { return null; }
            }
            return resultImage;
        }
        protected Color calculateNewPixelColorAvg(Bitmap sourceImage, int x, int y,
            float Avg, float AvgR, float AvgG, float AvgB)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(Clamp((int)((float)sourceColor.R * Avg / AvgR), 0, 255),
                                                Clamp((int)((float)sourceColor.G * Avg / AvgG), 0, 255),
                                                Clamp((int)((float)sourceColor.B * Avg / AvgB), 0, 255));
            return resultColor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }
    }



    class Autolevel
    {
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            int RMax = 0, RMin = 255, GMax = 0, GMin = 255, BMax = 0, BMin = 255;   
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color pixelColor = sourceImage.GetPixel(i, j);
                    if(pixelColor.R > RMax)
                    {
                        RMax = pixelColor.R;
                    }
                    if (pixelColor.G > GMax)
                    {
                        GMax = pixelColor.G;
                    }
                    if (pixelColor.B > BMax)
                    {
                        BMax = pixelColor.B;
                    }
                    if (pixelColor.R < RMin)
                    {
                        RMin = pixelColor.R;
                    }
                    if (pixelColor.G < GMin)
                    {
                        GMin = pixelColor.G;
                    }
                    if (pixelColor.B < BMin)
                    {
                        BMin = pixelColor.B;
                    }
                }
            }
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColorAuto(sourceImage, i, j, RMax, GMax, BMax, RMin, GMin, BMin));
                }
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending) { return null; }
            }
            return resultImage;
        }
        protected Color calculateNewPixelColorAuto(Bitmap sourceImage, int x, int y,
            int RMax, int GMax, int BMax, int RMin, int GMin, int BMin)
        {
            Color sourceColor = sourceImage.GetPixel(x, y); 
            Color resultColor = Color.FromArgb(Clamp((int)((float)(sourceColor.R - RMin) * ((255.0f - 0.0f) / (float)(RMax - RMin))), 0, 255),
                                                Clamp((int)((float)(sourceColor.G - GMin) * ((255.0f - 0.0f) / (float)(GMax - GMin))), 0, 255),
                                                 Clamp((int)((float)(sourceColor.B - BMin) * ((255.0f - 0.0f) / (float)(BMax - BMin))), 0, 255));
            return resultColor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }
    }



    class PerfectReflector
    {
        public Bitmap processImage(Bitmap sourceImage, BackgroundWorker worker)
        {
            Bitmap resultImage = new Bitmap(sourceImage.Width, sourceImage.Height);
            int RMax = 0, RMin = 255, GMax = 0, GMin = 255, BMax = 0, BMin = 255;
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    Color pixelColor = sourceImage.GetPixel(i, j);
                    if (pixelColor.R > RMax)
                    {
                        RMax = pixelColor.R;
                    }
                    if (pixelColor.G > GMax)
                    {
                        GMax = pixelColor.G;
                    }
                    if (pixelColor.B > BMax)
                    {
                        BMax = pixelColor.B;
                    }
                }
            }
            for (int i = 0; i < sourceImage.Width; i++)
            {
                for (int j = 0; j < sourceImage.Height; j++)
                {
                    resultImage.SetPixel(i, j, calculateNewPixelColor(sourceImage, i, j, RMax, GMax, BMax));
                }
                worker.ReportProgress((int)((float)i / resultImage.Width * 100));
                if (worker.CancellationPending) { return null; }
            }
            return resultImage;
        }

        protected Color calculateNewPixelColor(Bitmap sourceImage, int x, int y,
           int RMax, int GMax, int BMax)
        {
            Color sourceColor = sourceImage.GetPixel(x, y);
            Color resultColor = Color.FromArgb(Clamp((int)((float)sourceColor.R * (255.0f / (float)(RMax))), 0, 255),
                                                Clamp((int)((float)sourceColor.G * (255.0f / (float)(GMax))), 0, 255),
                                                 Clamp((int)((float)sourceColor.B * (255.0f / (float)(BMax))), 0, 255));
            return resultColor;
        }
        public int Clamp(int value, int min, int max)
        {
            if (value < min) { return min; }
            if (value > max) { return max; }
            return value;
        }
    }

    class Dilation : MatrixFilter
    {
        public Dilation()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            kernel[0, 0] = 0.0f;
            kernel[0, 1] = 1.0f;
            kernel[0, 2] = 0.0f;
            kernel[1, 0] = 1.0f;
            kernel[1, 1] = 1.0f;
            kernel[1, 2] = 1.0f;
            kernel[2, 0] = 0.0f;
            kernel[2, 1] = 1.0f;
            kernel[2, 2] = 0.0f;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            int maxR = 0;
            int maxG = 0;
            int maxB = 0;

            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    if (kernel[k + radiusX, l + radiusY] != 0)
                    {
                        int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                        int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                        Color neigbourColor = sourceImage.GetPixel(idX, idY);
                        maxR = Math.Max(maxR, neigbourColor.R);
                        maxG = Math.Max(maxG, neigbourColor.G);
                        maxB = Math.Max(maxB, neigbourColor.B);
                    }
                }
            }
            return Color.FromArgb(maxR, maxG, maxB);
        }

    }
    class Erosion : MatrixFilter
    {
        public Erosion()
        {
            int sizeX = 3;
            int sizeY = 3;
            kernel = new float[sizeX, sizeY];
            kernel[0, 0] = 0.0f;
            kernel[0, 1] = 1.0f;
            kernel[0, 2] = 0.0f;
            kernel[1, 0] = 1.0f;
            kernel[1, 1] = 1.0f;
            kernel[1, 2] = 1.0f;
            kernel[2, 0] = 0.0f;
            kernel[2, 1] = 1.0f;
            kernel[2, 2] = 0.0f;
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            int minR = 255;
            int minG = 255;
            int minB = 255;

            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    if (kernel[k + radiusX, l + radiusY] != 0)
                    {
                        int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                        int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                        Color neigbourColor = sourceImage.GetPixel(idX, idY);
                        minR = Math.Min(minR, neigbourColor.R);
                        minG = Math.Min(minG, neigbourColor.G);
                        minB = Math.Min(minB, neigbourColor.B);
                    }
                }
            }
            return Color.FromArgb(minR, minG, minB);
        }
    }

    public class MedianFilter : MatrixFilter
    {
        public MedianFilter()
        {
            int size = 3;
            kernel = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    kernel[i, j] = 1;
                }
            }
        }

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernel.GetLength(0) / 2;
            int radiusY = kernel.GetLength(1) / 2;
            List<int> RedList = new List<int>();
            List<int> GreenList = new List<int>();
            List<int> BlueList = new List<int>();
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neigbourColor = sourceImage.GetPixel(idX, idY);
                    RedList.Add(neigbourColor.R);
                    GreenList.Add(neigbourColor.G);
                    BlueList.Add(neigbourColor.B);
                }
            }
            RedList.Sort();
            GreenList.Sort();
            BlueList.Sort();

            return Color.FromArgb(RedList[RedList.Count / 2],
                                    GreenList[GreenList.Count / 2],
                                    BlueList[BlueList.Count / 2]);
        }
    }



    public class TwoMatrixFilter : Filters
    {
        protected float[,] kernelX = null;
        protected float[,] kernelY = null;
        protected TwoMatrixFilter() { }
        public TwoMatrixFilter(float[,] kernelX, float[,] kernelY) { this.kernelX = kernelX; this.kernelY = kernelY;}

        protected override Color calculateNewPixelColor(Bitmap sourceImage, int x, int y)
        {
            int radiusX = kernelX.GetLength(0) / 2;
            int radiusY = kernelY.GetLength(1) / 2;
            float resultXR = 0.0f, resultXG = 0.0f, resultXB = 0.0f;
            float resultYR = 0.0f, resultYG = 0.0f, resultYB = 0.0f;
            for (int l = -radiusY; l <= radiusY; l++)
            {
                for (int k = -radiusX; k <= radiusX; k++)
                {
                    int idX = Clamp(x + k, 0, sourceImage.Width - 1);
                    int idY = Clamp(y + l, 0, sourceImage.Height - 1);
                    Color neigbourColor = sourceImage.GetPixel(idX, idY);
                    resultXR += neigbourColor.R * kernelX[k + radiusX, l + radiusY];
                    resultXG += neigbourColor.G * kernelX[k + radiusX, l + radiusY];
                    resultXB += neigbourColor.B * kernelX[k + radiusX, l + radiusY];
                    resultYR += neigbourColor.R * kernelY[k + radiusX, l + radiusY];
                    resultYG += neigbourColor.G * kernelY[k + radiusX, l + radiusY];
                    resultYB += neigbourColor.B * kernelY[k + radiusX, l + radiusY];

                }
            }
            int gradientR = Clamp((int)Math.Sqrt(resultXR * resultXR + resultYR * resultYR), 0, 255);
            int gradientG = Clamp((int)Math.Sqrt(resultXG * resultXG + resultYG * resultYG), 0, 255);
            int gradientB = Clamp((int)Math.Sqrt(resultXB * resultXB + resultYB * resultYB), 0, 255);
            return Color.FromArgb(gradientR, gradientG, gradientB);
        }
    }

    public class SobelFilter : TwoMatrixFilter
    {
        public SobelFilter() {
            int size = 3;
            kernelY = new float[size, size];
            kernelY[0, 0] = -1.0f;
            kernelY[0, 1] = -2.0f;
            kernelY[0, 2] = -1.0f;
            kernelY[1, 0] = 0.0f;
            kernelY[1, 1] = 0.0f;
            kernelY[1, 2] = 0.0f;
            kernelY[2, 0] = 1.0f;
            kernelY[2, 1] = 2.0f;
            kernelY[2, 2] = 1.0f;
            kernelX = new float[size, size];
            kernelX[0, 0] = -1.0f;
            kernelX[0, 1] = 0.0f;
            kernelX[0, 2] = 1.0f;
            kernelX[1, 0] = -2.0f;
            kernelX[1, 1] = 0.0f;
            kernelX[1, 2] = 2.0f;
            kernelX[2, 0] = -1.0f;
            kernelX[2, 1] = 0.0f;
            kernelX[2, 2] = 1.0f;
        }
    }

    public class ScharrFilter : TwoMatrixFilter
    {
        public ScharrFilter()
        {
            int size = 3;
            kernelY = new float[size, size];
            kernelY[0, 0] = -3.0f;
            kernelY[0, 1] = -10.0f;
            kernelY[0, 2] = -3.0f;
            kernelY[1, 0] = 0.0f;
            kernelY[1, 1] = 0.0f;
            kernelY[1, 2] = 0.0f;
            kernelY[2, 0] = 3.0f;
            kernelY[2, 1] = 10.0f;
            kernelY[2, 2] = 3.0f;
            kernelX = new float[size, size];
            kernelX[0, 0] = -3.0f;
            kernelX[0, 1] = 0.0f;
            kernelX[0, 2] = 3.0f;
            kernelX[1, 0] = -10.0f;
            kernelX[1, 1] = 0.0f;
            kernelX[1, 2] = 10.0f;
            kernelX[2, 0] = -3.0f;
            kernelX[2, 1] = 0.0f;
            kernelX[2, 2] = 3.0f;
        }
    }

}
