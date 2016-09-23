using System;
using System.Collections.Generic;
using ImageTracerNet.Extensions;

namespace ImageTracerNet.Vectorization.Points
{
    internal class InterpolationPoint : Point<double>
    {
        public Heading Direction { get; set; } = Heading.Center;

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

        private static int GetDirectionKey(double value1, double value2)
        {
            //https://msdn.microsoft.com/en-us/library/fyxd1d26(v=vs.110).aspx
            return value1.AreEqual(value2) ? 0 : value1.CompareTo(value2);
        }

        private Heading CalculateDirection(InterpolationPoint other)
        {
            return Directions[Tuple.Create(GetDirectionKey(X, other.X), GetDirectionKey(Y, other.Y))];
        }

        public InterpolationPoint(PathPoint point1, PathPoint point2)
        {
            X = (point1.X + point2.X) / 2.0;
            Y = (point1.Y + point2.Y) / 2.0;
        }

        public InterpolationPoint(PathPoint point1, PathPoint point2, InterpolationPoint other)
            : this(point1, point2)
        {
            Direction = CalculateDirection(other);
        }
    }
}
