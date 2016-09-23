using System.Collections.Generic;

namespace ImageTracerNet.Vectorization
{
    internal static class Sequencing
    {
        private static int DetermineSequenceEndIndex(IReadOnlyList<Heading> directions, int initialIndex)
        {
            var initialDirection = directions[initialIndex];
            var lastIndex = directions.Count - 1;

            //var sequenceEndIndex = initialIndex + 1;
            int sequenceEndIndex;
            Heading? storedDirection = null;
            for (sequenceEndIndex = initialIndex + 1; sequenceEndIndex < lastIndex; ++sequenceEndIndex)
            {
                if (!(storedDirection == null || directions[sequenceEndIndex] == storedDirection || directions[sequenceEndIndex] == initialDirection))
                {
                    break;
                }

                if (directions[sequenceEndIndex] != initialDirection && storedDirection == null)
                {
                    storedDirection = directions[sequenceEndIndex];
                }
            }

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
