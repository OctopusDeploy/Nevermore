using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

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
        }

        public RelationalStore Store { get; set; }
        
        public RelationalStoreConfiguration RelationalStoreConfiguration { get; private set; }

        internal void CreateDatabase()
        {
            ExecuteScript(@"create database [" + TestDatabaseName + "] COLLATE SQL_Latin1_General_CP1_CS_AS", GetMaster(TestDatabaseConnectionString));
        }

        internal void DropDatabase()
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

        internal void InitializeStore(IEnumerable<DocumentMap> documentMaps, IEnumerable<CustomTypeDefinitionBase> customTypeDefinitions)
        {
            Store = BuildRelationalStore(TestDatabaseConnectionString, documentMaps, customTypeDefinitions, 0.01);
        }

        RelationalStore BuildRelationalStore(string connectionString, IEnumerable<DocumentMap> documentMaps, IEnumerable<CustomTypeDefinitionBase> customTypeDefinitions, double chaosFactor = 0.2D)
        {
            var config = new RelationalStoreConfiguration();
            RelationalStoreConfiguration = config;

            config.Initialize(new List<DocumentMap>()
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
                    new MachineToTestSerializationMap()
                }.Union(documentMaps),
                new []
                {
                    new EndpointTypeDefinition()
                }.Union(customTypeDefinitions ?? Enumerable.Empty<CustomTypeDefinitionBase>()));
            
            var sqlCommandFactory = chaosFactor > 0D
                ? (ISqlCommandFactory)new ChaosSqlCommandFactory(new SqlCommandFactory(config), chaosFactor)
                : new SqlCommandFactory(config);
            
            return new RelationalStore(connectionString ?? TestDatabaseConnectionString,
                TestDatabaseName,
                sqlCommandFactory,
                config,
                new EmptyRelatedDocumentStore());
        }

        internal void InstallSchema(IEnumerable<DocumentMap> documentMaps, IEnumerable<CustomTypeDefinitionBase> customTypeDefinitions)
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(Store);

            var output = new StringBuilder();
            var relationalStoreConfiguration = new RelationalStoreConfiguration();
            
            SchemaGenerator.WriteTableSchema(new CustomerMap(), null, output);

            // needed to generate the table
            var mappings = new DocumentMap[]
                {
                    new OrderMap(),
                    new CustomerMap(),
                    new SpecialProductMap(),
                    new LineItemMap(),
                    new BrandMap(),
                    new MachineMap()
                }
                .Union(documentMaps)
                .ToArray();

            relationalStoreConfiguration.Initialize(mappings, customTypeDefinitions);

            // needed for products, but not to generate the table
            relationalStoreConfiguration.Initialize(new DocumentMap[]{new ProductMap<Product>()});

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