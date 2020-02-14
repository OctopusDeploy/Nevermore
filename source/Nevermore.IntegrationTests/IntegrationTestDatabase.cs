using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Resources;
using System.IO;
using System.Text;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Nevermore.Serialization;
using Newtonsoft.Json;

namespace Nevermore.IntegrationTests
{
    public class IntegrationTestDatabase
    {
        readonly TextWriter output = Console.Out;
        readonly string SqlInstance = Environment.GetEnvironmentVariable("NevermoreTestServer") ?? "(local)\\SQLEXPRESS,1433";
        readonly string Username = Environment.GetEnvironmentVariable("NevermoreTestUsername");
        readonly string Password = Environment.GetEnvironmentVariable("NevermoreTestPassword");
        readonly string TestDatabaseName;
        readonly string TestDatabaseConnectionString;

        static IntegrationTestDatabase()
        {
            TransientFaultHandling.InitializeRetryManager();
        }

        public IntegrationTestDatabase()
        {
            TestDatabaseName = "Nevermore-IntegrationTests";
            
            var builder = new SqlConnectionStringBuilder($"Server={SqlInstance};Database={TestDatabaseName};{(Username == null ? "Trusted_connection=true;" : string.Empty)}")
            {
                ApplicationName = TestDatabaseName,
            };
            if (Username != null)
            {
                builder.UserID = Username;
                builder.Password = Password;
            }
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
            ExecuteScript(@"create database [" + TestDatabaseName + "] COLLATE SQL_Latin1_General_CP1_CS_AS", GetMaster(TestDatabaseConnectionString));
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
                new CustomerToTestSerializationMap(),
                new BrandMap(),
                new BrandToTestSerializationMap(),
                new ProductMap<Product>(),
                new SpecialProductMap(),
                new ProductToTestSerializationMap(),
                new LineItemMap(),
                new MachineMap(),
                new MachineToTestSerializationMap(),
                new FeedMap()
            });
            Store = BuildRelationalStore(TestDatabaseConnectionString, 0.01);
        }

        RelationalStore BuildRelationalStore(string connectionString, double chaosFactor = 0.2D)
        {
            var sqlCommandFactory = chaosFactor > 0D
                ? (ISqlCommandFactory)new ChaosSqlCommandFactory(new SqlCommandFactory(), chaosFactor)
                : new SqlCommandFactory();


            var contractResolver = new RelationalJsonContractResolver(Mappings);

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple
            };
            jsonSerializerSettings.Converters.Add(new ProductConverter(Mappings));
            jsonSerializerSettings.Converters.Add(new BrandConverter(Mappings));
            jsonSerializerSettings.Converters.Add(new EndpointConverter());
            jsonSerializerSettings.Converters.Add(new FeedConverter());
            jsonSerializerSettings.Converters.Add(new FeedTypeConverter());

            return new RelationalStore(connectionString ?? TestDatabaseConnectionString, TestDatabaseName, sqlCommandFactory, Mappings, jsonSerializerSettings, new EmptyRelatedDocumentStore());
        }

        void InstallSchema()
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(Store);

            var output = new StringBuilder();
            SchemaGenerator.WriteTableSchema(new CustomerMap(), null, output);

            // needed for products, but not to generate the table
            Mappings.Install(new List<DocumentMap>() { new ProductMap<Product>() });

            // needed to generate the table
            var mappings = new DocumentMap[]
            {
                new OrderMap(),
                new CustomerMap(),
                new SpecialProductMap(),
                new LineItemMap(),
                new BrandMap(),
                new MachineMap(),
                new FeedMap()
            };

            Mappings.Install(mappings);

            using (var transaction = Store.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                output.Clear();

                foreach (var map in mappings)
                    SchemaGenerator.WriteTableSchema(map, null, output);

                transaction.ExecuteScalar<int>(output.ToString());

                transaction.ExecuteScalar<int>($"alter table [{nameof(Customer)}] add [RowVersion] rowversion");

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