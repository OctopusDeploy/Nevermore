using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace Nevermore.IntegrationTests
{
    public static class IntegrationTestDatabase
    {
        static readonly string SqlInstance = ConfigurationManager.AppSettings["SqlServerInstance"] ?? "(local)\\SQLEXPRESS";
        static readonly string TestDatabaseName;
        static readonly string TestDatabaseConnectionString;
        static readonly IRelationalStoreFactory StorageEngine;

        static IntegrationTestDatabase()
        {

            TestDatabaseName = "Nevermore-IntegrationTests";

            var builder = new SqlConnectionStringBuilder(string.Format("Server={0};Database={1};Trusted_connection=true;", SqlInstance, TestDatabaseName))
            {
                ApplicationName = TestDatabaseName
            };
            TestDatabaseConnectionString = builder.ToString();

            StorageEngine = new RelationalStoreFactory(TestDatabaseConnectionString, TestDatabaseName);

            DropDatabase();
            CreateDatabase();

            InitializeStore();
            InstallSchema();
        }

        static IEnumerable<DocumentMap> GetMappings()
        {
            return new List<DocumentMap>();
        }

        public static RelationalStore Store { get; set; }
        public static RelationalMappings Mappings { get; set; }

        static void CreateDatabase()
        {
            ExecuteScript(@"create database [" + TestDatabaseName + "]", GetMaster(TestDatabaseConnectionString));
        }

        static void DropDatabase()
        {
            try
            {
                Console.WriteLine("Connecting to the 'master' database at " + TestDatabaseConnectionString);
                Console.WriteLine("Dropping " + TestDatabaseName);
                ExecuteScript("ALTER DATABASE [" + TestDatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; drop database [" + TestDatabaseName + "]", GetMaster(TestDatabaseConnectionString));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not drop the existing database: " + ex.GetErrorSummary());
            }
        }

        static void InitializeStore()
        {
            Mappings = RelationalStoreFactory.CreateMappings();
            Store = StorageEngine.RelationalStore;
        }

        static void InstallSchema()
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(Store);
        }

        public static void ExecuteScript(string script, string connectionString = null)
        {
            using (var connection = new SqlConnection(connectionString ?? TestDatabaseConnectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(script, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        static string GetMaster(string sqlConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(sqlConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ToString();
        }
    }
}