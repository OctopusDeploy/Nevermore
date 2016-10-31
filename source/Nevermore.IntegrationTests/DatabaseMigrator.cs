using System;
using System.Data.SqlClient;
using DbUp;
using DbUp.Engine.Output;

namespace Nevermore.IntegrationTests
{
    public class DatabaseMigrator
    {
        private readonly IUpgradeLog _log;

        public DatabaseMigrator(IUpgradeLog log = null)
        {
            _log = log ?? new ConsoleUpgradeLog();
        }

        public void Migrate(IRelationalStore store)
        {
            var upgrader =
                DeployChanges.To
                    .SqlDatabase(store.ConnectionString)
                    .WithScriptsAndCodeEmbeddedInAssembly(typeof(IntegrationTestDatabase).Assembly)
                    .LogScriptOutput()
                    .WithVariable("databaseName", new SqlConnectionStringBuilder(store.ConnectionString).InitialCatalog)
                    .LogTo(_log)
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw new Exception("Database migration failed: " + result.Error.GetErrorSummary(), result.Error);
            }
        }
    }
}
