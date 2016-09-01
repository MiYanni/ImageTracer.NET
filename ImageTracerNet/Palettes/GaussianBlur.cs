using System;
using System.Drawing;
using System.Threading.Tasks;
using ImageTracerNet.Extensions;

namespace ImageTracerNet.Palettes
{
    //http://stackoverflow.com/questions/33569396/correctly-implement-a-2-pass-gaussian-blur
    internal class GaussianBlur
    {
        private int kernelSize;
        private float sigma;
        private float[,] KernelX;
        private float[,] KernelY;

        public GaussianBlur(float sigma = 10)
        {
            this.kernelSize = ((int)Math.Ceiling(sigma) * 2) + 1;
            this.sigma = sigma;
            KernelX = CreateGaussianKernel(true);
            KernelY = CreateGaussianKernel(false);
        }

        /// <summary>
        /// Create a 1 dimensional Gaussian kernel using the Gaussian G(x) function
        /// </summary>
        /// <param name="horizontal">Whether to calculate a horizontal kernel.</param>
        /// <returns>The <see cref="T:float[,]"/></returns>
        private float[,] CreateGaussianKernel(bool horizontal)
        {
            int size = this.kernelSize;
            float[,] kernel = horizontal ? new float[1, size] : new float[size, 1];
            float sum = 0.0f;

            float midpoint = (size - 1) / 2f;
            for (int i = 0; i < size; i++)
            {
                float x = i - midpoint;
                float gx = this.Gaussian(x);
                sum += gx;
                if (horizontal)
                {
                    kernel[0, i] = gx;
                }
                else
                {
                    kernel[i, 0] = gx;
                }
            }

            // Normalise kernel so that the sum of all weights equals 1
            if (horizontal)
            {
                for (int i = 0; i < size; i++)
                {
                    kernel[0, i] = kernel[0, i] / sum;
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    kernel[i, 0] = kernel[i, 0] / sum;
                }
            }

            return kernel;
        }

        /// <summary>
        /// Implementation of 1D Gaussian G(x) function
        /// </summary>
        /// <param name="x">The x provided to G(x)</param>
        /// <returns>The Gaussian G(x)</returns>
        private float Gaussian(float x)
        {
            const float Numerator = 1.0f;
            float deviation = this.sigma;
            float denominator = (float)(Math.Sqrt(2 * Math.PI) * deviation);

            float exponentNumerator = -x * x;
            float exponentDenominator = (float)(2 * Math.Pow(deviation, 2));

            float left = Numerator / denominator;
            float right = (float)Math.Exp(exponentNumerator / exponentDenominator);

            return left * right;
        }

        /// <inheritdoc/>
        public void Apply(
            Bitmap target,
            Bitmap source,
            Rectangle sourceRectangle,
            int startY,
            int endY)
        {
            float[,] kernelX = this.KernelX;
            float[,] kernelY = this.KernelY;

            Bitmap firstPass = new Bitmap(source.Width, source.Height);
            this.ApplyConvolution(firstPass, source, sourceRectangle, startY, endY, kernelX);
            this.ApplyConvolution(target, firstPass, sourceRectangle, startY, endY, kernelY);
        }

        /// <summary>
        /// Applies the process to the specified portion of the specified <see cref="ImageBase"/> at the specified location
        /// and with the specified size.
        /// </summary>
        /// <param name="target">Target image to apply the process to.</param>
        /// <param name="source">The source image. Cannot be null.</param>
        /// <param name="sourceRectangle">
        /// The <see cref="Rectangle"/> structure that specifies the portion of the image object to draw.
        /// </param>
        /// <param name="startY">The index of the row within the source image to start processing.</param>
        /// <param name="endY">The index of the row within the source image to end processing.</param>
        /// <param name="kernel">The kernel operator.</param>
        private void ApplyConvolution(
            Bitmap target,
            Bitmap source,
            Rectangle sourceRectangle,
            int startY,
            int endY,
            float[,] kernel)
        {
            int kernelHeight = kernel.GetLength(0);
            int kernelWidth = kernel.GetLength(1);
            int radiusY = kernelHeight >> 1;
            int radiusX = kernelWidth >> 1;

            int sourceY = sourceRectangle.Y;
            int sourceBottom = sourceRectangle.Bottom;
            int startX = sourceRectangle.X;
            int endX = sourceRectangle.Right;
            int maxY = sourceBottom - 1;
            int maxX = endX - 1;

            for(var y = startY; y < endY; ++y)
                {
                    if (y >= sourceY && y < sourceBottom)
                    {
                        for (int x = startX; x < endX; x++)
                        {
                            float red = 0;
                            float green = 0;
                            float blue = 0;
                            float alpha = 0;

                    // Apply each matrix multiplier to the color components for each pixel.
                    for (int fy = 0; fy < kernelHeight; fy++)
                            {
                                int fyr = fy - radiusY;
                                int offsetY = y + fyr;

                                offsetY = offsetY.Clamp(0, maxY);

                                for (int fx = 0; fx < kernelWidth; fx++)
                                {
                                    int fxr = fx - radiusX;
                                    int offsetX = x + fxr;

                                    offsetX = offsetX.Clamp(0, maxX);

                                    Color currentColor = source.GetPixel(offsetX, offsetY);

                                    red += kernel[fy, fx] * currentColor.R;
                                    green += kernel[fy, fx] * currentColor.G;
                                    blue += kernel[fy, fx] * currentColor.B;
                                    alpha += kernel[fy, fx] * currentColor.A;
                                }
                            }

                            target.SetPixel(x, y, Color.FromArgb((int)alpha, (int)red, (int)green, (int)blue));
                        }
                    }
                }
        }
    }
}
