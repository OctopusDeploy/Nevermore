using System;
using Nevermore.Advanced.InstanceTypeResolvers;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductTypeResolver : IInstanceTypeResolver
    {
        public Type ResolveTypeFromValue(Type baseType, object typeColumnValue)
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

        public object ResolveValueFromType(Type type)
        {
            if (typeof(Product).IsAssignableFrom(type))
            {
                if (type == typeof(DodgyProduct)) return ProductType.Dodgy;
                if (type == typeof(SpecialProduct)) return ProductType.Special;
            }

            return null;
        }
    }
}