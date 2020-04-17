using System;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Hooks
{
    public interface IHook
    {
        void BeforeInsert(object document, DocumentMap map, IWriteTransaction transaction) {}
        void AfterInsert(object document, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeUpdate(object document, DocumentMap map, IWriteTransaction transaction) {}
        void AfterUpdate(object document, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeDelete(string id, DocumentMap map, IWriteTransaction transaction) {}
        void AfterDelete(string id, DocumentMap map, IWriteTransaction transaction) {}
        void BeforeCommit(IWriteTransaction transaction) {}
        void AfterCommit(IWriteTransaction transaction) {}

        Task BeforeInsertAsync(object document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterInsertAsync(object document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeUpdateAsync(object document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterUpdateAsync(object document, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeDeleteAsync(string id, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterDeleteAsync(string id, DocumentMap map, IWriteTransaction transaction) => Task.CompletedTask;
        Task BeforeCommitAsync(IWriteTransaction transaction) => Task.CompletedTask;
        Task AfterCommitAsync(IWriteTransaction transaction) => Task.CompletedTask;
    }
}