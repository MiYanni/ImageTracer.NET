using System;
using System.Collections.Generic;

namespace ImageTracerNet.Vectorization
{
    //https://en.wikipedia.org/wiki/Directed_graph
    internal class DirectedEdge
    {
        public EdgeNode Node { get; set; }
        public WalkDirection Direction { get; set; }

        #region Equality
        protected bool Equals(DirectedEdge other)
        {
            return Node == other.Node && Direction == other.Direction;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((DirectedEdge) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) Node*397) ^ (int) Direction;
            }
        }
        #endregion
    }

    internal static class DirectedEdgeExtensions
    {
        public static void Add(this IList<DirectedEdge> list, EdgeNode node, WalkDirection direction)
        {
            list.Add(new DirectedEdge { Node = node, Direction = direction });
        }
    }
}
