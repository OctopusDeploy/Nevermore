using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced
{
    public interface IConnectionAndTransaction : IDisposable
    {
        DbConnection Connection { get; }
        DbTransaction Transaction { get; }
        void CommitTransaction();
        Task CommitTransactionAsync(CancellationToken cancellationToken);
    }
}