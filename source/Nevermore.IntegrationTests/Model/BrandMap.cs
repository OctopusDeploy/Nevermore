using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class BrandMap : DocumentHierarchyMap<Brand>
    {
        public BrandMap(RelationalStoreConfiguration relationalStoreConfiguration) : base(relationalStoreConfiguration)
        {
            Column(m => m.Name);
        }

        readonly Dictionary<string, Type> derivedTypeMappings = new Dictionary<string, Type>
        {
            {BrandA.BrandType, typeof(BrandA)},
            {BrandB.BrandType, typeof(BrandB)}
        };

        protected override IDictionary<string, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }

    public class BrandToTestSerialization : IDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public string JSON { get; set; }
    }

    public class BrandToTestSerializationMap : DocumentMap<BrandToTestSerialization>
    {
        public BrandToTestSerializationMap(RelationalStoreConfiguration relationalStoreConfiguration) : base(relationalStoreConfiguration)
        {
            TableName = "Brand";
            Column(x => x.Name);
            Column(x => x.JSON);
        }
    }

}