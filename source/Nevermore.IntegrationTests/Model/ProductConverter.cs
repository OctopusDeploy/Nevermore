using System;
using System.Collections.Generic;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Mapping;
using Nevermore.Serialization;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductTypeResolver : IInstanceTypeResolver
    {
        public Type Resolve(Type baseType, object typeColumnValue)
        {
            if (typeof(Product).IsAssignableFrom(baseType) && typeColumnValue is ProductType productType)
            {
                // Note that these could easily be split into three different IInstanceTypeResolver classes.
                if (productType == ProductType.Dodgy) return typeof(DodgyProduct);
                if (productType == ProductType.Special) return typeof(SpecialProduct);
                if (productType == ProductType.Normal) return typeof(Product);
            }

            return null;
        }
    }
}