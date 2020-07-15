using System.Collections.Generic;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Hooks
{
    internal class HookRegistry : IHookRegistry
    {
        readonly List<IHook> hooks = new List<IHook>();

        public void Register(IHook hook)
        {
            hooks.Add(hook);
        }
        
        public void BeforeInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.BeforeInsert(document, map, transaction);
        }

        public void AfterInsert<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.AfterInsert(document, map, transaction);
        }

        public void BeforeUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.BeforeUpdate(document, map, transaction);
        }

        public void AfterUpdate<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.AfterUpdate(document, map, transaction);
        }

        public void BeforeDelete<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.BeforeDelete<TDocument>(id, map, transaction);
        }

        public void AfterDelete<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) hook.AfterDelete<TDocument>(id, map, transaction);
        }

        public void BeforeCommit(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.BeforeCommit(transaction);
        }

        public void AfterCommit(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.AfterCommit(transaction);
        }

        public async Task BeforeInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.BeforeInsertAsync(document, map, transaction);
        }

        public async Task AfterInsertAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.AfterInsertAsync(document, map, transaction);
        }

        public async Task BeforeUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.BeforeUpdateAsync(document, map, transaction);
        }

        public async Task AfterUpdateAsync<TDocument>(TDocument document, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.AfterUpdateAsync(document, map, transaction);
        }

        public async Task BeforeDeleteAsync<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.BeforeDeleteAsync<TDocument>(id, map, transaction);
        }

        public async Task AfterDeleteAsync<TDocument>(object id, DocumentMap map, IWriteTransaction transaction) where TDocument : class
        {
            foreach (var hook in hooks) await hook.AfterDeleteAsync<TDocument>(id, map, transaction);
        }

        public async Task BeforeCommitAsync(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.BeforeCommitAsync(transaction);
        }

        public async Task AfterCommitAsync(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.AfterCommitAsync(transaction);
        }
    }
}