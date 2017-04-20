using System;
using Nevermore.Mapping;
using Nevermore.Contracts;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests
{
    public abstract class FixtureWithRelationalStore
    {
        readonly IntegrationTestDatabase integrationTestDatabase;

        protected FixtureWithRelationalStore(ITestOutputHelper output)
        {
            integrationTestDatabase = new IntegrationTestDatabase(output);

            integrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
            integrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"DELETE FROM ?\"");
            integrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
            integrationTestDatabase.Store.Reset();
        }

        protected IRelationalStore Store => integrationTestDatabase.Store;

        protected RelationalMappings Mappings => integrationTestDatabase.Mappings;

        public int CountOf<T>() where T : class, IId
        {
            return InTransaction(s => s.Query<T>().Count());
        }

        public void StoreAll<T>(params T[] items) where T : class, IId
        {
            InTransaction(s =>
            {
                foreach (var item in items) s.Insert(item);
            });
        }

        public void InTransaction(Action<IRelationalTransaction> callback)
        {
            var transaction = Store.BeginTransaction();
            callback(transaction);
            transaction.Commit();
            transaction.Dispose();
        }

        public TReturn InTransaction<TReturn>(Func<IRelationalTransaction, TReturn> callback)
        {
            var transaction = Store.BeginTransaction();
            var result = callback(transaction);
            transaction.Commit();
            transaction.Dispose();
            return result;
        }
    }
}