using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageTracerNet.Vectorization
{
    internal static class Sequencing
    {
        //private static int Original(IReadOnlyList<Heading> directions, int initialIndex)
        //{
        //    var initialDirection = directions[initialIndex];
        //    var lastIndex = directions.Count - 1;

        //    int sequenceEndIndex;
        //    Heading? storedDirection = null;
        //    for (sequenceEndIndex = initialIndex + 1; sequenceEndIndex < lastIndex; ++sequenceEndIndex)
        //    {
        //        var direction = directions[sequenceEndIndex];
        //        if (!(storedDirection == null || direction == storedDirection || direction == initialDirection))
        //        {
        //            return sequenceEndIndex;
        //        }

        //        // This initializes storedDirection at some point during the loop.
        //        if (storedDirection == null && direction != initialDirection)
        //        {
        //            storedDirection = direction;
        //        }
        //    }

        //    return 0;
        //}

        private static int DetermineSequenceEndIndex(IReadOnlyList<Heading> directions, int initialIndex)
        {
            var initialDirection = directions[initialIndex];
            var lastIndex = directions.Count - 1;
            Heading? storedDirection = null;
            //http://stackoverflow.com/questions/8867867/sequence-contains-no-elements-exception-in-linq-without-even-using-single
            var sequenceEndIndex = directions.Select((d, i) => new { Direction = d, Index = i }).Where(di => di.Index >= initialIndex + 1 && di.Index <= lastIndex)
                .DefaultIfEmpty(new { Direction = Heading.Center, Index = lastIndex })
                .Aggregate((di, diNext) =>
                {
                    if (!(storedDirection == null || di.Direction == storedDirection || di.Direction == initialDirection))
                    {
                        return di;
                    }

                    // This initializes storedDirection at some point during the loop.
                    if (storedDirection == null && di.Direction != initialDirection)
                    {
                        storedDirection = di.Direction;
                    }

                    return diNext;
                }).Index;

            // If it gets to the end of the list, return zero instead of the index.
            return sequenceEndIndex == lastIndex ? 0 : sequenceEndIndex;
        }

        public static IEnumerable<SequenceIndices> Create(IReadOnlyList<Heading> directions)
        {
            int sequenceEndIndex;
            for (var i = 0; i < directions.Count; i = sequenceEndIndex)
            {
                sequenceEndIndex = DetermineSequenceEndIndex(directions, i);
                yield return new SequenceIndices { Start = i, End = sequenceEndIndex };

                // If the end index is the last index of the list (will be 0 from DetermineSequenceEndIndex), then we have all of the sequences.
                if (!(sequenceEndIndex > 0))
                {
                    yield break;
                }
            }
        }
    }
}
