using System;
using System.Data;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeDefinition
    {
        public virtual DbType DbType => DbType.String;
        public virtual int MaxLength => 250;

        public abstract Type TypeToConvert { get; }
    }
}