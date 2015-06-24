using System;

namespace Nevermore.IntegrationTests
{
    public class FixtureWithRelationalStore : IDisposable
    {
        public IRelationalStore Store
        {
            get { return IntegrationTestDatabase.Store; }
        }

        public RelationalMappings Mappings
        {
            get { return IntegrationTestDatabase.Mappings; }
        }

        public FixtureWithRelationalStore()
        {
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"DELETE FROM ?\"");
            IntegrationTestDatabase.ExecuteScript("EXEC sp_msforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
            IntegrationTestDatabase.Store.Reset();
        }

        void IDisposable.Dispose()
        {
        }
        
        public int CountOf<T>() where T : class
        {
            return InTransaction(s => s.Query<T>().Count());
        }

        public void StoreAll<T>(params T[] items) where T : class
        {
            InTransaction(s =>
            {
                foreach (var item in items) s.Insert(item);
            });
        }

        public void InTransaction(Action<IRelationalTransaction> callback)
        {
            var session = Store.BeginTransaction();
            callback(session);
            session.Commit();
            session.Dispose();
        }

        public TReturn InTransaction<TReturn>(Func<IRelationalTransaction, TReturn> callback)
        {
            var session = Store.BeginTransaction();
            var result = callback(session);
            session.Commit();
            session.Dispose();
            return result;
        }
    }
}