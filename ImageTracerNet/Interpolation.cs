using System;
using System.Collections.Generic;
using ImageTracerNet.Extensions;

namespace ImageTracerNet
{
    internal static class Interpolation
    {
        
        private static readonly Dictionary<Tuple<int, int>, Heading> Directions = new Dictionary<Tuple<int, int>, Heading>
        {
            {-1, -1, Heading.SouthEast},
            {-1,  1, Heading.NorthEast},
            {-1,  0, Heading.East},
            {1,  -1, Heading.SouthWest},
            {1,   1, Heading.NorthWest},
            {1,   0, Heading.West},
            {0,  -1, Heading.South},
            {0,   1, Heading.North},
            {0,   0, Heading.Center}
        };

        private static int ToDirectionKey(this double value1, double value2)
        {
            //https://msdn.microsoft.com/en-us/library/fyxd1d26(v=vs.110).aspx
            return value1.AreEqual(value2) ? 0 : value1.CompareTo(value2);
        }

        // 4. interpolating between path points for nodes with 8 directions ( East, SouthEast, S, SW, W, NW, N, NE )
        public static List<List<double[]>> Convert(List<List<PathPoint>> paths)
        {
            var ins = new List<List<double[]>>();

            // paths loop
            foreach (var path in paths)
            {
                var thisInp = new List<double[]>();
                ins.Add(thisInp);
                var pathLength = path.Count;
                // pathpoints loop
                for (var pointIndex = 0; pointIndex < pathLength; pointIndex++)
                {
                    var pp1 = path[pointIndex];
                    // interpolate between two path points
                    var pp2 = path[(pointIndex + 1) % pathLength];
                    var pp3 = path[(pointIndex + 2) % pathLength];

                    var thisPoint = new double[3];
                    thisPoint[0] = (pp1.X + pp2.X) / 2.0;
                    thisPoint[1] = (pp1.Y + pp2.Y) / 2.0;
                    thisInp.Add(thisPoint);

                    var nextPoint = new double[2];
                    nextPoint[0] = (pp2.X + pp3.X) / 2.0;
                    nextPoint[1] = (pp2.Y + pp3.Y) / 2.0;

                    // line segment direction to the next point
                    var pointComparison = new Tuple<int, int>(thisPoint[0].ToDirectionKey(nextPoint[0]), thisPoint[1].ToDirectionKey(nextPoint[1]));
                    thisPoint[2] = (double) Directions[pointComparison];

                    //if (thisPoint[0] < nextPoint[0])
                    //{
                    //    if (thisPoint[1] < nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.SouthEast;
                    //    }// SouthEast
                    //    else if (thisPoint[1] > nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.NorthEast;
                    //    } // NE
                    //    else
                    //    {
                    //        thisPoint[2] = (double)Heading.East;
                    //    } // E
                    //}
                    //else if (thisPoint[0] > nextPoint[0])
                    //{
                    //    if (thisPoint[1] < nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.SouthWest;
                    //    }// SW
                    //    else if (thisPoint[1] > nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.NorthWest;
                    //    } // NW
                    //    else
                    //    {
                    //        thisPoint[2] = (double)Heading.West;
                    //    }// N
                    //}
                    //else
                    //{
                    //    if (thisPoint[1] < nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.South;
                    //    }// S
                    //    else if (thisPoint[1] > nextPoint[1])
                    //    {
                    //        thisPoint[2] = (double)Heading.North;
                    //    } // N
                    //    else
                    //    {
                    //        thisPoint[2] = (double)Heading.Center;
                    //    }// center, this should not happen
                    //}
                }// End of pathpoints loop
            }

            return ins;
        }
    }
}
