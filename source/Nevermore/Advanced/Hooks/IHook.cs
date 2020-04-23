using System;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Hooks
{
    public interface IHook
    {
        void BeforeInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) {}
        void AfterInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) {}
        void AfterUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeDelete<TDocument>(string id, DocumentMap map, IWriteTransaction transaction) {}
        void AfterDelete<TDocument>(string id, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeCommit(IWriteTransaction transaction) {}
        void AfterCommit(IWriteTransaction transaction) {}

        Task BeforeInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeDeleteAsync<TDocument>(string id, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterDeleteAsync<TDocument>(string id, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeCommitAsync(IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterCommitAsync(IWriteTransaction transaction) => Task.CompletedTask;
    }
}