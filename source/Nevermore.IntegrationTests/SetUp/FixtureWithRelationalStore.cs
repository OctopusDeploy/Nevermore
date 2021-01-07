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
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.CommandFactory = new ChaosSqlCommandFactory(new SqlCommandFactory());
            config.ApplicationName = "Nevermore-IntegrationTests";
            config.DefaultSchema = "TestSchema";
            config.DocumentMaps.Register(
                new CustomerMap(),
                new BrandMap(),
                new ProductMap(),
                new LineItemMap(),
                new MachineMap(),
                new OrderMap(),
                new MessageWithStringIdMap(),
                new MessageWithIntIdMap(),
                new MessageWithLongIdMap(),
                new MessageWithGuidIdMap());
            
            config.TypeHandlers.Register(new ReferenceCollectionTypeHandler());
            config.InstanceTypeResolvers.Register(new ProductTypeResolver());
            config.InstanceTypeResolvers.Register(new BrandTypeResolver());
            
            config.UseJsonNetSerialization(settings =>
            {
                settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            });
            
            GenerateSchemaAutomatically(
                new OrderMap(), 
                new ProductMap(),
                new CustomerMap(), 
                new LineItemMap(), 
                new BrandMap(), 
                new MachineMap(),
                new MessageWithStringIdMap(),
                new MessageWithIntIdMap(),
                new MessageWithLongIdMap(),
                new MessageWithGuidIdMap());
            
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

        void GenerateSchemaAutomatically(params IDocumentMap[] mappings)
        {
            try
            {
                var schema = new StringBuilder();
                foreach (var map in mappings)
                {
                    SchemaGenerator.WriteTableSchema(map.Build(), null, schema);
                }
                schema.AppendLine($"alter table [TestSchema].[{nameof(Customer)}] add [RowVersion] rowversion");
                integrationTestDatabase.ExecuteScript(schema.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating schema for tests: " + ex.Message, ex);
            }
        }
    }
}