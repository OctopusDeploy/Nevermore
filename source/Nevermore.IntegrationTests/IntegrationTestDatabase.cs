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
        readonly string SqlInstance = Environment.GetEnvironmentVariable("NevermoreTestServer") ?? "(local)";
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
        
        public RelationalStoreConfiguration RelationalStoreConfiguration { get; private set; }

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
            Store = BuildRelationalStore(TestDatabaseConnectionString, 0.01);
        }

        RelationalStore BuildRelationalStore(string connectionString, double chaosFactor = 0.2D)
        {
            var config = new RelationalStoreConfiguration(CustomTypeDefinitions());
            RelationalStoreConfiguration = config;

            config.AddDocumentMaps(new List<DocumentMap>()
            {
                new CustomerMap(config),
                new CustomerToTestSerializationMap(config),
                new BrandMap(config),
                new BrandToTestSerializationMap(config),
                new ProductMap<Product>(config),
                new SpecialProductMap(config),
                new ProductToTestSerializationMap(config),
                new LineItemMap(config),
                new MachineMap(config),
                new MachineToTestSerializationMap(config)
            });

            var mappings = AddCustomMappings(config);
            config.AddDocumentMaps(mappings);
            
            var sqlCommandFactory = chaosFactor > 0D
                ? (ISqlCommandFactory)new ChaosSqlCommandFactory(new SqlCommandFactory(config), chaosFactor)
                : new SqlCommandFactory(config);
            
            config.AddCustomTypeDefinitions(new []
            {
                new EndpointTypeDefinition()
            });
            
            return new RelationalStore(connectionString ?? TestDatabaseConnectionString,
                TestDatabaseName,
                sqlCommandFactory,
                config,
                new EmptyRelatedDocumentStore());
        }

        protected virtual IEnumerable<DocumentMap> AddCustomMappings(RelationalStoreConfiguration config)
        {
            return AddCustomMappingsForSchemaGeneration(config);
        }

        protected virtual IEnumerable<DocumentMap> AddCustomMappingsForSchemaGeneration(RelationalStoreConfiguration config)
        {
            return Enumerable.Empty<DocumentMap>();
        }

        protected virtual IEnumerable<ICustomTypeDefinition> CustomTypeDefinitions()
        {
            return null;
        }

        void InstallSchema()
        {
            Console.WriteLine("Performing migration");
            var migrator = new DatabaseMigrator();
            migrator.Migrate(Store);

            var output = new StringBuilder();
            var relationalStoreConfiguration = new RelationalStoreConfiguration(CustomTypeDefinitions());
            
            SchemaGenerator.WriteTableSchema(new CustomerMap(relationalStoreConfiguration), null, output);

            // needed for products, but not to generate the table
            relationalStoreConfiguration.AddDocumentMaps(new List<DocumentMap>() { new ProductMap<Product>(relationalStoreConfiguration) });

            // needed to generate the table
            var mappings = new DocumentMap[]
                {
                    new OrderMap(relationalStoreConfiguration),
                    new CustomerMap(relationalStoreConfiguration),
                    new SpecialProductMap(relationalStoreConfiguration),
                    new LineItemMap(relationalStoreConfiguration),
                    new BrandMap(relationalStoreConfiguration),
                    new MachineMap(relationalStoreConfiguration)
                }
                .Union(AddCustomMappingsForSchemaGeneration(relationalStoreConfiguration))
                .ToArray();

            relationalStoreConfiguration.AddDocumentMaps(mappings);

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