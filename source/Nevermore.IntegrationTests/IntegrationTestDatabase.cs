using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests
{
    public class IntegrationTestDatabase
    {
        readonly ITestOutputHelper output;
        readonly string SqlInstance = "(local)\\SQLEXPRESS";
        readonly string TestDatabaseName;
        readonly string TestDatabaseConnectionString;

        static IntegrationTestDatabase()
        {
            TransientFaultHandling.InitializeRetryManager();
        }

        public IntegrationTestDatabase(ITestOutputHelper output)
        {
            this.output = output;

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

        public RelationalStore Store { get; set; }
        public RelationalMappings Mappings { get; set; }

        void CreateDatabase()
        {
            ExecuteScript(@"create database [" + TestDatabaseName + "]", GetMaster(TestDatabaseConnectionString));
        }

        void DropDatabase()
        {
            try
            {
                output.WriteLine("Connecting to the 'master' database at " + TestDatabaseConnectionString);
                output.WriteLine("Dropping " + TestDatabaseName);
                ExecuteScript("ALTER DATABASE [" + TestDatabaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; drop database [" + TestDatabaseName + "]", GetMaster(TestDatabaseConnectionString));
            }
            catch (Exception ex)
            {
                output.WriteLine("Could not drop the existing database: " + ex.GetErrorSummary());
            }
        }

        void InitializeStore()
        {
            Mappings = new RelationalMappings();
            Mappings.Install(new List<DocumentMap>()
            {
                new CustomerMap(),
                new ProductMap(),
                new LineItemMap()
            });
            Store = BuildRelationalStore(TestDatabaseConnectionString, 0.01);
        }

        RelationalStore BuildRelationalStore(string connectionString, double chaosFactor = 0.2D)
        {
            var sqlCommandFactory = chaosFactor > 0D
                ? (ISqlCommandFactory)new ChaosSqlCommandFactory(new SqlCommandFactory(), chaosFactor)
                : new SqlCommandFactory();

            return new RelationalStore(connectionString ?? TestDatabaseConnectionString, TestDatabaseName, sqlCommandFactory, Mappings, new JsonSerializerSettings(), new EmptyRelatedDocumentStore());
        }

        void InstallSchema()
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(Store);

            var output = new StringBuilder();
            SchemaGenerator.WriteTableSchema(new CustomerMap(), null, output);

            var mappings = new DocumentMap[]
            {
                new CustomerMap(),
                new ProductMap(),
                new LineItemMap()
            };

            Mappings.Install(mappings);

            using (var transaction = Store.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                output.Clear();

                foreach (var map in mappings)
                    SchemaGenerator.WriteTableSchema(map, null, output);

                transaction.ExecuteScalar<int>(output.ToString());

                transaction.Commit();
            }
        }

        public void ExecuteScript(string script, string connectionString = null)
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

        string GetMaster(string sqlConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(sqlConnectionString)
            {
                InitialCatalog = "master"
            };
            return builder.ToString();
        }
    }
}