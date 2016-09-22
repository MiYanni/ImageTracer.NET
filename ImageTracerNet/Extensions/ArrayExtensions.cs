using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet.Extensions
{
    public static class ArrayExtensions
    {
        //public static T[][] InitInner<T>(this T[][] jagged, int length)
        //{
        //    for (var i = 0; i < jagged.GetLength(0); ++i)
        //    {
        //        jagged[i] = new T[length];
        //    }
        //    return jagged;
        //}

        //public static T[][][] InitInner<T>(this T[][][] jagged, int length1, int length2)
        //{
        //    for (var i = 0; i < jagged.GetLength(0); ++i)
        //    {
        //        jagged[i] = new T[length1][].InitInner(length2);
        //    }
        //    return jagged;
        //}

        public static T[][] InitInner<T>(this T[][] jagged, int length)
        {
            return jagged.Initialize(() => new T[length]);
        }

        public static T[][][] InitInner<T>(this T[][][] jagged, int length1, int length2)
        {
            return jagged.Initialize(() => new T[length1][].InitInner(length2));
        }

        public static T[][] SetDefault<T>(this T[][] jagged) where T: struct
        {
            foreach (var inner in jagged)
            {
                inner.Initialize(default(T));
            }
            return jagged;
        }

        // Do not use with reference types as every cell would be initialized with the same reference.
        // Used the Func overloads below with reference types.
        public static T[] Initialize<T>(this T[] array, T value) where T: struct
        {
            return array.Initialize(() => value);
        }

        public static T[] Initialize<T>(this T[] array, T value, params int[] indices) where T : struct
        {
            return array.Initialize(i => value, indices);
        }

        public static T[] Initialize<T>(this T[] array, Func<T> initializer)
        {
            return array.Initialize(i => initializer());
        }

        public static T[] Initialize<T>(this T[] array, Func<int, T> initializer)
        {
            Parallel.For(0, array.Length, i => array[i] = initializer(i));
            return array;
        }

        public static T[] Initialize<T>(this T[] array, Func<int, T> initializer, params int[] indices)
        {
            Parallel.ForEach(indices, i => array[i] = initializer(i));
            return array;
        }
    }
}
