﻿using System;
using System.Drawing;
using System.Linq;

namespace ImageTracerNet.Extensions
{
    public static class ColorExtensions
    {
        public static Color[] FromRgbaByteArray(byte[] data)
        {
            return data.Select((comp, i) => new { Color = i / 4, Component = comp })
                .GroupBy(x => x.Color, x => x.Component).Select(comps =>
                    Color.FromArgb(comps.ElementAt(3), comps.ElementAt(0), comps.ElementAt(1), comps.ElementAt(2)))
                .ToArray();
        }

        private static readonly Random Rng = new Random();
        public static Color RandomColor()
        {
            return FromRgbaByteArray(Enumerable.Range(0, 4).Select(i => (byte)Math.Floor(Rng.NextDouble() * 255)).ToArray()).Single();
        }

        //https://en.wikipedia.org/wiki/Rectilinear_distance
        public static int CalculateRectilinearDistance(this Color first, Color second)
        {
            var firstArray = first.ToRgbaByteArray();
            var secondArray = second.ToRgbaByteArray();
            // weighted alpha seems to help images with transparency
            return firstArray.Zip(secondArray, (f, s) => Math.Abs(f - s)).Select((d, i) => i == 3 ? d * 4 : d).Sum();
        }

        public static byte[] ToRgbaByteArray(this Color[] colors)
        {
            return colors.Select(c => c.ToRgbaByteArray()).SelectMany(b => b).ToArray();
        }

        public static byte[] ToRgbaByteArray(this Color color)
        {
            return new[] { color.R, color.G, color.B, color.A };
        }

        /// <summary>
        /// Creates a Color from alpha, hue, saturation and brightness.
        /// http://stackoverflow.com/a/4106615/294804
        /// </summary>
        /// <param name="alpha">The alpha channel value.</param>
        /// <param name="hue">The hue value.</param>
        /// <param name="saturation">The saturation value.</param>
        /// <param name="brightness">The brightness value.</param>
        /// <returns>A Color with the given values.</returns>
        public static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
        {
            if (0 > alpha
                || 255 < alpha)
            {
                throw new ArgumentOutOfRangeException(
                    "alpha",
                    alpha,
                    "Value must be within a range of 0 - 255.");
            }

            if (0f > hue
                || 360f < hue)
            {
                throw new ArgumentOutOfRangeException(
                    "hue",
                    hue,
                    "Value must be within a range of 0 - 360.");
            }

            if (0f > saturation
                || 1f < saturation)
            {
                throw new ArgumentOutOfRangeException(
                    "saturation",
                    saturation,
                    "Value must be within a range of 0 - 1.");
            }

            if (0f > brightness
                || 1f < brightness)
            {
                throw new ArgumentOutOfRangeException(
                    "brightness",
                    brightness,
                    "Value must be within a range of 0 - 1.");
            }

            if (0 == saturation)
            {
                return Color.FromArgb(
                                    alpha,
                                    Convert.ToInt32(brightness * 255),
                                    Convert.ToInt32(brightness * 255),
                                    Convert.ToInt32(brightness * 255));
            }

            float fMax, fMid, fMin;
            int iSextant, iMax, iMid, iMin;

            if (0.5 < brightness)
            {
                fMax = brightness - (brightness * saturation) + saturation;
                fMin = brightness + (brightness * saturation) - saturation;
            }
            else
            {
                fMax = brightness + (brightness * saturation);
                fMin = brightness - (brightness * saturation);
            }

            iSextant = (int)Math.Floor(hue / 60f);
            if (300f <= hue)
            {
                hue -= 360f;
            }

            hue /= 60f;
            hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
            if (0 == iSextant % 2)
            {
                fMid = (hue * (fMax - fMin)) + fMin;
            }
            else
            {
                fMid = fMin - (hue * (fMax - fMin));
            }

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant)
            {
                case 1:
                    return Color.FromArgb(alpha, iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(alpha, iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(alpha, iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(alpha, iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(alpha, iMax, iMin, iMid);
                default:
                    return Color.FromArgb(alpha, iMax, iMid, iMin);
            }
        }

        private const int RgbMax = 255;
        //http://stackoverflow.com/questions/1165107/how-do-i-invert-a-colour-color
        public static Color Invert(this Color color)
        {
            return Color.FromArgb(RgbMax - color.R, RgbMax - color.G, RgbMax - color.B);
        }
    }
}
