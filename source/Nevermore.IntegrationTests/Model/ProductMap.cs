using System;
using System.Collections.Generic;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductMap : DocumentMap<Product>
    {
        static readonly Dictionary<ProductType, Type> SubTypeMappings = new Dictionary<ProductType, Type>
        {
            {ProductType.Special, typeof(SpecialProduct)},
            {ProductType.Dodgy, typeof(DodgyProduct)},
            {ProductType.Normal, typeof(Product)}
        };

        public ProductMap()
        {
            
            Column(m => m.Name);
            TypeDiscriminatorColumn(m => m.Type, SubTypeMappings);
        }
    }
}