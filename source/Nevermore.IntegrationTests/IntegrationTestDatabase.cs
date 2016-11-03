using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nevermore.IntegrationTests
{
    public static class IntegrationTestDatabase
    {
        static readonly string SqlInstance = ConfigurationManager.AppSettings["SqlServerInstance"] ?? "(local)\\SQLEXPRESS";
        static readonly string TestDatabaseName;
        static readonly string TestDatabaseConnectionString;

        static IntegrationTestDatabase()
        {
            TransientFaultHandling.InitializeRetryManager();

            TestDatabaseName = "Nevermore-IntegrationTests";

            var builder = new SqlConnectionStringBuilder(string.Format("Server={0};Database={1};Trusted_connection=true;", SqlInstance, TestDatabaseName))
            {
                ApplicationName = TestDatabaseName
            };
            TestDatabaseConnectionString = builder.ToString();

            DropDatabase();
            CreateDatabase();

            InitializeStore();
            InstallSchema();
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
            Mappings = new RelationalMappings();
            Mappings.Install(new List<DocumentMap>());
            Store = BuildRelationalStore(TestDatabaseConnectionString, 0.01);
        }

        static RelationalStore BuildRelationalStore(string connectionString, double chaosFactor = 0.2D)
        {
            var sqlCommandFactory = chaosFactor > 0D
                ? (ISqlCommandFactory)new ChaosSqlCommandFactory(new SqlCommandFactory(), chaosFactor)
                : new SqlCommandFactory();
            
            return new RelationalStore(connectionString ?? TestDatabaseConnectionString, TestDatabaseName, sqlCommandFactory, Mappings, new JsonSerializerSettings(), new EmptyRelatedDocumentStore());
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