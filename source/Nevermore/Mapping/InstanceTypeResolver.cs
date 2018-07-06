using System;
using System.Data;

namespace Nevermore.Mapping
{
    public abstract class InstanceTypeResolver
    {
        public abstract Type GetTypeFromInstance(object instance);
        public abstract Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal);
    }
}