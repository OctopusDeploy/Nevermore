using System;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Hooks
{
    public interface IHook
    {
        void BeforeInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void AfterInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void BeforeUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void AfterUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void BeforeDelete<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void AfterDelete<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class {}
        void BeforeCommit(IWriteTransaction transaction) {}
        void AfterCommit(IWriteTransaction transaction) {}

        Task BeforeInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task AfterInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task BeforeUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task AfterUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task BeforeDeleteAsync<TDocument>(object id, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task AfterDeleteAsync<TDocument>(object id, DocumentMap map, IWriteTransaction transaction, CancellationToken cancellationToken = default) where TDocument : class => Task.CompletedTask;
        Task BeforeCommitAsync(IWriteTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
        Task AfterCommitAsync(IWriteTransaction transaction, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}