using System;
using System.Text;
using Nevermore.Advanced.Serialization;
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
            config.Mappings.Register(
                new CustomerMap(),
                new BrandMap(),
                new ProductMap(),
                new LineItemMap(),
                new MachineMap(),
                new OrderMap());
            
            config.TypeHandlerRegistry.Register(new ReferenceCollectionTypeHandler());
            config.InstanceTypeRegistry.Register(new ProductTypeResolver());
            
            config.UseJsonNetSerialization(settings =>
            {
                settings.Converters.Add(new BrandConverter(config.Mappings));
                settings.Converters.Add(new EndpointConverter());
                settings.ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor;
            });
            
            GenerateSchemaAutomatically(
                new OrderMap(), 
                new ProductMap(),
                new CustomerMap(), 
                new LineItemMap(), 
                new BrandMap(), 
                new MachineMap());
            
            Store = new RelationalStore(config);
        }

        public IRelationalStore Store { get; }
        public RelationalStoreConfiguration Configuration => Store.Configuration;
        public IDocumentMapRegistry Mappings => Configuration.Mappings;

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

        void GenerateSchemaAutomatically(params DocumentMap[] mappings)
        {
            try
            {
                var schema = new StringBuilder();
                foreach (var map in mappings)
                {
                    SchemaGenerator.WriteTableSchema(map, null, schema);
                }
                schema.AppendLine($"alter table [{nameof(Customer)}] add [RowVersion] rowversion");
                integrationTestDatabase.ExecuteScript(schema.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating schema for tests: " + ex.Message, ex);
            }
        }
    }
}