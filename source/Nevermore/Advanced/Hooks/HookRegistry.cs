using System.Collections.Generic;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore.Advanced.Hooks
{
    internal class HookRegistry : IHookRegistry, IHook
    {
        readonly List<IHook> hooks = new List<IHook>();

        public void Register(IHook hook)
        {
            hooks.Add(hook);
        }
        
        public void BeforeInsert(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.BeforeInsert(document, map, transaction);
        }

        public void AfterInsert(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.AfterInsert(document, map, transaction);
        }

        public void BeforeUpdate(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.BeforeUpdate(document, map, transaction);
        }

        public void AfterUpdate(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.AfterUpdate(document, map, transaction);
        }

        public void BeforeDelete(string id, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.BeforeDelete(id, map, transaction);
        }

        public void AfterDelete(string id, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.AfterDelete(id, map, transaction);
        }

        public void BeforeCommit(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.BeforeCommit(transaction);
        }

        public void AfterCommit(IWriteTransaction transaction)
        {
            foreach (var hook in hooks) hook.AfterCommit(transaction);
        }

        public async Task BeforeInsertAsync(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.BeforeInsertAsync(document, map, transaction);
        }

        public async Task AfterInsertAsync(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.AfterInsertAsync(document, map, transaction);
        }

        public async Task BeforeUpdateAsync(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.BeforeUpdateAsync(document, map, transaction);
        }

        public async Task AfterUpdateAsync(object document, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.AfterUpdateAsync(document, map, transaction);
        }

        public async Task BeforeDeleteAsync(string id, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.BeforeDeleteAsync(id, map, transaction);
        }

        public async Task AfterDeleteAsync(string id, DocumentMap map, IWriteTransaction transaction)
        {
            foreach (var hook in hooks) await hook.AfterDeleteAsync(id, map, transaction);
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