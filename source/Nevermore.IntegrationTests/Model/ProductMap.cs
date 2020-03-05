using System;
using System.Collections.Generic;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Serialization;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductMap<TProduct> : DocumentMap<TProduct> where TProduct : Product
    {
        public ProductMap()
        {
            Column(m => m.Name);
            Column(m => m.Type);
        }
    }

    public class SpecialProductMap : ProductMap<SpecialProduct>
    {
        public SpecialProductMap()
        {
            TableName = typeof(Product).Name;
            Column(m => m.BonusMaterial).IsNullable = true;
        }
    }

    public class ProductToTestSerialization : IDocument
    {
        public ProductToTestSerializationId Id { get; set; }
        public string Name { get; set; }
     
        public string Type { get; set; }
        public string JSON { get; set; }
    }

    public class ProductToTestSerializationId : CaseSensitiveTypedString
    {
        public ProductToTestSerializationId(string value) : base(value)
        {
        }
    }

    public class ProductToTestSerializationMap : DocumentMap<ProductToTestSerialization>
    {
        public ProductToTestSerializationMap()
        {
            TableName = "Product";
            Column(x => x.Name);
            Column(x => x.Type);
            Column(x => x.JSON);
        }
    }

    public class ProductConverter : InheritedClassConverter<Product, ProductType>
    {
        readonly Dictionary<ProductType, Type> derivedTypeMappings = new Dictionary<ProductType, Type>
        {
            {ProductType.Dodgy, typeof(DodgyProduct)},
            {ProductType.Special, typeof(SpecialProduct)},
            {ProductType.Normal, typeof(Product)}
        };

        public ProductConverter(RelationalMappings relationalMappings) : base(relationalMappings)
        {
        }

        protected override IDictionary<ProductType, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }
}