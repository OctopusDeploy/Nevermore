using System;
using System.Reflection;
using DbUp;
using DbUp.Engine.Output;
using Microsoft.Data.SqlClient;
using Nevermore.Util;

namespace Nevermore.IntegrationTests.SetUp
{
    public class DatabaseMigrator
    {
        readonly IUpgradeLog log;

        public DatabaseMigrator(IUpgradeLog log = null)
        {
            this.log = log ?? new ConsoleUpgradeLog();
        }

        public void Migrate(string connectionString)
        {
            var upgrader =
                DeployChanges.To
                    .SqlDatabase(connectionString)
                    .WithScriptsAndCodeEmbeddedInAssembly(typeof(RelationalStore).GetTypeInfo().Assembly)
                    .WithScriptsAndCodeEmbeddedInAssembly(typeof(IntegrationTestDatabase).GetTypeInfo().Assembly)
                    .LogScriptOutput()
                    .WithVariable("databaseName", new SqlConnectionStringBuilder(connectionString).InitialCatalog)
                    .LogTo(log)
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw new Exception("Database migration failed: " + result.Error.GetErrorSummary(), result.Error);
            }
        }
    }
}
