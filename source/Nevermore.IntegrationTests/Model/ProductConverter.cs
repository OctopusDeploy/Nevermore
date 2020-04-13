using System;
using System.Collections.Generic;
using Nevermore.Mapping;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductConverter : InheritedClassConverter<Product, ProductType>
    {
        readonly Dictionary<ProductType, Type> derivedTypeMappings = new Dictionary<ProductType, Type>
        {
            {ProductType.Dodgy, typeof(DodgyProduct)},
            {ProductType.Special, typeof(SpecialProduct)},
            {ProductType.Normal, typeof(Product)}
        };

        public ProductConverter(IDocumentMapRegistry documentMapRegistry) : base(documentMapRegistry)
        {
        }

        protected override IDictionary<ProductType, Type> DerivedTypeMappings => derivedTypeMappings;
        protected override string TypeDesignatingPropertyName => "Type";
    }
}