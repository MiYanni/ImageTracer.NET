using System;
using System.Collections.Generic;

namespace ImageTracerNet.Vectorization
{
    internal static class Sequencing
    {
        private static int DetermineSequenceEndIndex(IReadOnlyList<Heading> directions, int initialIndex)
        {
            var initialDirection = directions[initialIndex];
            var lastIndex = directions.Count - 1;

            int sequenceEndIndex;
            Heading? storedDirection = null;
            for (sequenceEndIndex = initialIndex + 1; sequenceEndIndex < lastIndex; ++sequenceEndIndex)
            {
                var direction = directions[sequenceEndIndex];
                if (!(storedDirection == null || direction == storedDirection || direction == initialDirection))
                {
                    return sequenceEndIndex;
                }

                // This initializes storedDirection at some point during the loop.
                if (storedDirection == null && direction != initialDirection)
                {
                    storedDirection = direction;
                }
            }

            // If it gets to the end of the list, return zero instead of the index.
            //return sequenceEndIndex == lastIndex ? 0 : sequenceEndIndex;
            return 0;
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
