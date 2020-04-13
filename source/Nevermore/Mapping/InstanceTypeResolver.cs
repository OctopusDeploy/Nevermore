using System;
using System.Data;

namespace Nevermore.Mapping
{
    // TODO: As far as I can tell, neither Octofront nor Octopus Deploy use this!
    public abstract class InstanceTypeResolver
    {
        public abstract Type GetTypeFromInstance(object instance);
        public abstract Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal);
    }
}