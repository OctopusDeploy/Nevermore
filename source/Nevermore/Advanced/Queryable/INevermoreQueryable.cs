using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Nevermore.Advanced.Queryable
{
    /// <summary>
    /// This interface can be used to determine whether an IQueryable is underpinned
    /// by Nevermore.
    /// </summary>
    /// <typeparam name="T">The queryable element type</typeparam>
    public interface INevermoreQueryable<out T> : IOrderedQueryable<T>
    {
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default);
    }
}