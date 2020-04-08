using System;
using System.Data;
using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeSerializationBase
    {
        public virtual DbType DbType => DbType.String;
        public virtual int MaxLength => 250;

        public abstract bool CanConvertType(Type type);

        internal abstract JsonConverter GetJsonConverter(RelationalMappings relationalMappings);
    }
}