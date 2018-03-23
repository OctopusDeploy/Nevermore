using System;
using System.Collections.Generic;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductMap<TProduct> : DocumentMap<TProduct> where TProduct : Product
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

        public class SpecialProductMap : ProductMap<SpecialProduct>
    {

        public SpecialProductMap()
        {
            TableName = typeof(Product).Name;
            Column(m => m.BonusMaterial).IsNullable = true;
        }
    }
}