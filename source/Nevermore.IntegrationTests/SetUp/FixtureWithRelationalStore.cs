using System;
using System.Text;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.IntegrationTests.Contracts;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.SetUp
{
    public abstract class FixtureWithRelationalStore : FixtureWithDatabase
    {
        bool resetBetweenTests = true;

        protected FixtureWithRelationalStore()
        {
            var documentMaps = new IDocumentMap[]
            {
                new CustomerMap(),
                new BrandMap(),
                new ProductMap(),
                new LineItemMap(),
                new MachineMap(),
                new OrderMap(),
                new MessageWithStringIdMap(),
                new MessageWithIntIdMap(),
                new MessageWithLongIdMap(),
                new MessageWithGuidIdMap(),
                new DocumentWithRowVersionMap(),
                new DocumentWithIdentityIdMap(),
                new DocumentWithIdentityIdAndRowVersionMap(),
                new DocumentWithCustomPrefixMap(),
                new DocumentWithCustomPrefixAndStringIdMap()
            };

            var config = new RelationalStoreConfiguration(ConnectionString)
            {
                CommandFactory = new ChaosSqlCommandFactory(new SqlCommandFactory()),
                ApplicationName = "Nevermore-IntegrationTests",
                DefaultSchema = "TestSchema"
            };

            config.TypeHandlers.Register(new ReferenceCollectionTypeHandler());
            config.TypeHandlers.Register(new StringCustomIdTypeHandler<CustomerId>());
            config.TypeHandlers.Register(new StringCustomIdTypeHandler<CustomPrefixId>());

            config.PrimaryKeyHandlers.Register(new StringCustomIdTypeIdKeyHandler<CustomerId>());
            config.PrimaryKeyHandlers.Register(new CustomPrefixIdKeyHandler());

            config.InstanceTypeResolvers.Register(new ProductTypeResolver());
            config.InstanceTypeResolvers.Register(new BrandTypeResolver());

            config.DocumentMaps.Register(documentMaps);

            config.UseJsonNetSerialization(settings =>
            {
                settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            });

            GenerateSchemaAutomatically(config, documentMaps);

            Store = new RelationalStore(config);
        }

        public IRelationalStore Store { get; }
        public IRelationalStoreConfiguration Configuration => Store.Configuration;
        public IDocumentMapRegistry Mappings => Configuration.DocumentMaps;

        protected void NoMonkeyBusiness()
        {
            Configuration.CommandFactory = new SqlCommandFactory();
        }

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
        }

        [SetUp]
        public virtual void SetUp()
        {
            if (resetBetweenTests)
            {
                integrationTestDatabase.ResetBetweenTestRuns();
                ((RelationalStore)Store).Reset();
            }
        }

        protected void KeepDataBetweenTests()
        {
            resetBetweenTests = false;
        }

        void GenerateSchemaAutomatically(RelationalStoreConfiguration configuration, params IDocumentMap[] mappings)
        {
            try
            {
                var schema = new StringBuilder();
                foreach (var map in mappings)
                {
                    SchemaGenerator.WriteTableSchema(map.Build(configuration.PrimaryKeyHandlers), null, schema);
                }
                integrationTestDatabase.ExecuteScript(schema.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating schema for tests: " + ex.Message, ex);
            }
        }
    }
}