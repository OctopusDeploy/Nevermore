using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Util
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> BatchWithBlockSize<T>(this IEnumerable<T> source, int blockSize)
        {
            return source
                .Select((x, index) => new { x, index })
                .GroupBy(x => x.index / blockSize, y => y.x);
        }
    }
}
