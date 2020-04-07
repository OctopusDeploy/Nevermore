using System;
using System.Data;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeDefinitionBase
    {
        public virtual DbType DbType => DbType.String;
        public virtual int MaxLength => 250;

        public abstract bool CanConvertType(Type type);
    }
}