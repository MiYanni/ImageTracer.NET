using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet.Extensions
{
    public static class MathExtensions
    {
        //http://stackoverflow.com/a/6598240/294804
        public static bool IsZero(this double value, double tolerance = .001)
        {
            return Math.Abs(value) < tolerance;
        }

        //http://stackoverflow.com/a/6598240/294804
        public static bool IsNotZero(this double value, double tolerance = .001)
        {
            return !value.IsZero(tolerance);
        }
    }
}
