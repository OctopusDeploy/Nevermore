using System;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public abstract class FixtureWithRelationalStore
    {
        protected IRelationalStore Store
        {
            get { return IntegrationTestDatabase.Store; }
        }

        protected RelationalMappings Mappings
        {
            get { return IntegrationTestDatabase.Mappings; }
        }

        [TestFixtureSetUp]
        public virtual void FixtureSetUp()
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"DELETE FROM ?\"");
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
            IntegrationTestDatabase.Store.Reset();
        }

        [TearDown]
        public virtual void TearDown()
        {
        }

        [TestFixtureTearDown]
        public virtual void FixtureTearDown()
        {
        }

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