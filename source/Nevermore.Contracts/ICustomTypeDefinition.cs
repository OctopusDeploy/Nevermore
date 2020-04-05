using System;
using System.Data;

namespace Nevermore.Contracts
{
    public interface ICustomTypeDefinition
    {
        Type ModelType { get; }
        
        DbType DbType { get; }
        int MaxLength { get; }

        object ToDbValue(object instance);
        object FromDbValue(object value);
    }

    public interface ICustomInheritedTypeDefinition : ICustomTypeDefinition
    {
        
    }
    public interface ICustomInheritedTypeDefinition<TDiscriminator> : ICustomTypeDefinition
        where TDiscriminator : struct
    {
        
    }
}