using System;

namespace Nevermore
{
    public interface IRelationalTransaction : IQueryExecutor, IDisposable
    {
        /// <summary>
        /// Commits the current pending transaction.
        /// </summary>
        void Commit();
    }
}