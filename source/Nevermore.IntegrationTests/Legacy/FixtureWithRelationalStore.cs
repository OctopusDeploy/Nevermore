using System;
using Nevermore.Contracts;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Legacy
{
    public abstract class FixtureWithRelationalStore
    {
        IntegrationTestDatabase integrationTestDatabase;

        [SetUp]
        public virtual void SetUp()
        {
            integrationTestDatabase = new IntegrationTestDatabase();

            integrationTestDatabase.ExecuteScript("EXEC sp_MSforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
            integrationTestDatabase.ExecuteScript("EXEC sp_MSforeachtable \"DELETE FROM ?\"");
            integrationTestDatabase.ExecuteScript("EXEC sp_MSforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
            integrationTestDatabase.Store.Reset();
        }

        protected IRelationalStore Store => integrationTestDatabase.Store;

        protected RelationalMappings Mappings => integrationTestDatabase.Mappings;

        public int CountOf<T>() where T : class, IId
        {
            return InTransaction(s => s.TableQuery<T>().Count());
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