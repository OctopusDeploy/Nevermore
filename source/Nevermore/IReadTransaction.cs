using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace Nevermore
{
    public interface IReadTransaction : IReadQueryExecutor, IDisposable
    {
        /// <summary>
        /// Allows hooks and other extensions to store data on this transaction that they can retrieve later.
        /// Not designed for typical usage. 
        /// </summary>
        IDictionary<string, object> State { get; }

        // SqlConnection connection { get; set; }
    }
}