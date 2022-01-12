using System.Linq;

namespace Nevermore.Advanced.Queryable
{
    /// <summary>
    /// This is a marker interface for determining whether an IQueryable is underpinned
    /// by Nevermore.
    /// </summary>
    /// <typeparam name="T">The queryable element type</typeparam>
    public interface INevermoreQueryable<T> : IOrderedQueryable<T>
    {
    }
}