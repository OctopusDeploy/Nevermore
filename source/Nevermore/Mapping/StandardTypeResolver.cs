using System;
using System.Data;

namespace Nevermore.Mapping
{
    class StandardTypeResolver : InstanceTypeResolver
    {
        readonly DocumentMap mapper;

        public StandardTypeResolver(DocumentMap mapper)
        {
            this.mapper = mapper;
        }

        public override Type GetTypeFromInstance(object instance)
        {
            return mapper.Type;
        }

        public override Func<IDataReader, Type> TypeResolverFromReader(Func<string, int> columnOrdinal)
        {
            return (reader) => mapper.Type;
        }
    }
}