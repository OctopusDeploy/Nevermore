using System;
using Microsoft.Data.SqlClient;
using Nevermore.Util;

namespace Nevermore.IntegrationTests.SetUp
{
    public class IntegrationTestDatabase
    {
        readonly string testDatabaseName;

        public IntegrationTestDatabase()
        {
            var sqlInstance = Environment.GetEnvironmentVariable("NevermoreTestServer") ?? "(local)\\SQLEXPRESS,1433";
            var username = Environment.GetEnvironmentVariable("NevermoreTestUsername");
            var password = Environment.GetEnvironmentVariable("NevermoreTestPassword");
            testDatabaseName = Environment.GetEnvironmentVariable("NevermoreTestDatabase") ?? "Nevermore-IntegrationTests";
            var builder = new SqlConnectionStringBuilder($"Server={sqlInstance};Database={testDatabaseName};{(username == null ? "Trusted_connection=true;" : string.Empty)}")
            {
                ApplicationName = testDatabaseName,
            };
            if (username != null)
            {
                builder.UserID = username;
                builder.Password = password;
            }

            ConnectionString = builder.ToString();

            DropDatabase();
            CreateDatabase();

            InstallSchema();
        }
        
        public string ConnectionString { get; }

        void DropDatabase()
        {
            try
            {
                Console.WriteLine("Connecting to the 'master' database at " + ConnectionString);
                Console.WriteLine("Dropping " + testDatabaseName);
                ExecuteScript("ALTER DATABASE [" + testDatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; drop database [" + testDatabaseName + "]", GetMaster());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not drop the existing database: " + ex.GetErrorSummary());
            }
        }
        
        void CreateDatabase()
        {
            ExecuteScript(@"create database [" + testDatabaseName + "] COLLATE SQL_Latin1_General_CP1_CS_AS", GetMaster());
        }

        void InstallSchema()
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(ConnectionString);
        }

        public void ResetBetweenTestRuns()
        {
            ExecuteScript("EXEC sp_MSforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\"");
            ExecuteScript("EXEC sp_MSforeachtable \"DELETE FROM ?\"");
            ExecuteScript("EXEC sp_MSforeachtable \"ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all\"");
        }

        public void ExecuteScript(string script, string connectionString = null)
        {
            using (var connection = new SqlConnection(connectionString ?? ConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(script, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        string GetMaster()
        {
            var builder = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ToString();
        }
    }
}