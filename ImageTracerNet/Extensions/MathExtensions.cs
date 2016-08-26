using System;

namespace ImageTracerNet.Extensions
{
    public static class MathExtensions
    {
        public const double DefaultTolerance = .001;

        //http://stackoverflow.com/a/6598240/294804
        public static bool IsZero(this double value, double tolerance = DefaultTolerance)
        {
            return Math.Abs(value) < tolerance;
        }

        //http://stackoverflow.com/a/6598240/294804
        public static bool IsNotZero(this double value, double tolerance = DefaultTolerance)
        {
            return !value.IsZero(tolerance);
        }

        public static bool AreEqual(this double value1, double value2, double tolerance = DefaultTolerance)
        {
            return Math.Abs(value1 - value2) < tolerance;
        }

        public static bool AreNotEqual(this double value1, double value2, double tolerance = DefaultTolerance)
        {
            return !value1.AreEqual(value2, tolerance);
        }
    }
}
