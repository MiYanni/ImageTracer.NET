using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet.Extensions
{
    public static class ArrayExtensions
    {
        public static T[][] InitInner<T>(this T[][] jagged, int length)
        {
            for (var i = 0; i < jagged.GetLength(0); ++i)
            {
                jagged[i] = new T[length];
            }
            return jagged;
        }

        public static T[][][] InitInner<T>(this T[][][] jagged, int length1, int length2)
        {
            for (var i = 0; i < jagged.GetLength(0); ++i)
            {
                jagged[i] = new T[length1][].InitInner(length2);
            }
            return jagged;
        }
    }
}
