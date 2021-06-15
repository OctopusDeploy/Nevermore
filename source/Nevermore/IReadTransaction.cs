using System;
using System.Collections.Generic;

namespace Nevermore
{
    public interface IReadTransaction : IReadQueryExecutor, IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Allows hooks and other extensions to store data on this transaction that they can retrieve later.
        /// Not designed for typical usage.
        /// </summary>
        IDictionary<string, object> State { get; }
    }
}