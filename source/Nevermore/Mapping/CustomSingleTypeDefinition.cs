using System;
using System.Data;

namespace Nevermore.Mapping
{
    public abstract class CustomSingleTypeDefinition : ICustomTypeDefinition
    {
        public abstract Type ModelType { get; }

        public virtual DbType DbType => DbType.String;
        public virtual int MaxLength => 250;

        public abstract object ToDbValue(object instance);
        public abstract object FromDbValue(object value);
    }
}