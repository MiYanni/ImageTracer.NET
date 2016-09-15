using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageTracerNet
{
    internal static class Sequencing
    {
        private static int DetermineSequenceEndIndex(IReadOnlyList<Heading> directions, int pathIndex)
        {
            var pathDirection = directions[pathIndex];
            var lastIndex = directions.Count - 1;

            var sequenceEndIndex = pathIndex + 1;
            Heading? storedDirection = null;
            for (var i = sequenceEndIndex; i < lastIndex; sequenceEndIndex = ++i)
            {
                if (!(storedDirection == null || directions[i] == storedDirection || directions[i] == pathDirection))
                {
                    break;
                }

                if (directions[i] != pathDirection && storedDirection == null)
                {
                    storedDirection = directions[i];
                }
            }

            // If it gets to the end of the list, return zero instead of the index.
            return sequenceEndIndex == lastIndex ? 0 : sequenceEndIndex;
        }

        public static IEnumerable<Tuple<int, int>> Create(IReadOnlyList<Heading> directions)
        {
            int sequenceEndIndex;
            for (var i = 0; i < directions.Count; i = sequenceEndIndex)
            {
                sequenceEndIndex = DetermineSequenceEndIndex(directions, i);
                yield return new Tuple<int, int>(i, sequenceEndIndex);

                // If the end index is the last index of the list (will be 0 from DetermineSequenceEndIndex), then we have all of the sequences.
                if (!(sequenceEndIndex > 0))
                {
                    yield break;
                }
            }
        }
    }
}
