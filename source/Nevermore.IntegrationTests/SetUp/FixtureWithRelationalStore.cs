using System.Text;
using Nevermore.IntegrationTests.Chaos;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.SetUp
{
    public abstract class FixtureWithRelationalStore : FixtureWithDatabase
    {
        protected FixtureWithRelationalStore()
        {
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.CommandFactory = new ChaosSqlCommandFactory(new SqlCommandFactory());
            config.ApplicationName = "Nevermore-IntegrationTests";
            config.Mappings.Register(
                new CustomerMap(),
                new CustomerToTestSerializationMap(),
                new BrandMap(),
                new BrandToTestSerializationMap(),
                new ProductMap<Product>(),
                new SpecialProductMap(),
                new ProductToTestSerializationMap(),
                new LineItemMap(),
                new MachineMap(),
                new OrderMap(),
                new MachineToTestSerializationMap());
            
            config.JsonSerializerSettings.Converters.Add(new ProductConverter(config.Mappings));
            config.JsonSerializerSettings.Converters.Add(new BrandConverter(config.Mappings));
            config.JsonSerializerSettings.Converters.Add(new EndpointConverter());
            
            GenerateSchemaAutomatically(
                new OrderMap(), 
                new CustomerMap(), 
                new SpecialProductMap(), 
                new LineItemMap(), 
                new BrandMap(), 
                new MachineMap());
            
            Store = new RelationalStore(config);
        }

        public IRelationalStore Store { get; }
        public RelationalStoreConfiguration Configuration => Store.Configuration;
        public IDocumentMapRegistry Mappings => Configuration.Mappings;

        [OneTimeSetUp]
        public virtual void OneTimeSetUp()
        {
        }
        
        [SetUp]
        public virtual void SetUp()
        {
            integrationTestDatabase.ResetBetweenTestRuns();
            ((RelationalStore)Store).Reset();
        }

        void GenerateSchemaAutomatically(params DocumentMap[] mappings)
        {
            var schema = new StringBuilder();
            foreach (var map in mappings)
            {
                SchemaGenerator.WriteTableSchema(map, null, schema);
            }
            schema.AppendLine($"alter table [{nameof(Customer)}] add [RowVersion] rowversion");
            integrationTestDatabase.ExecuteScript(schema.ToString());
        }
    }
}