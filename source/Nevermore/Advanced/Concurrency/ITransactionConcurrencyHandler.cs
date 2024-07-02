using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced.Concurrency
{
    public interface ITransactionConcurrencyHandler : IDisposable
    {
        IDisposable Lock();
        
        Task<IDisposable> LockAsync(CancellationToken cancellationToken);
    }
}