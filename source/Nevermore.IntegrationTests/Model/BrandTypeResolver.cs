using System;
using Nevermore.Advanced.InstanceTypeResolvers;

namespace Nevermore.IntegrationTests.Model
{
    public class BrandTypeResolver : IInstanceTypeResolver
    {
        public Type Resolve(Type baseType, object typeColumnValue)
        {
            if (typeof(Brand).IsAssignableFrom(baseType) && typeColumnValue is string type)
            {
                if (type == BrandA.BrandType) return typeof(BrandA);
                if (type == BrandB.BrandType) return typeof(BrandB);
            }

            return null;
        }
    }
}