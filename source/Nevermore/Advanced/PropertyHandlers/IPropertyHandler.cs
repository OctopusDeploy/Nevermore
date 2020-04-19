using Nevermore.Advanced.TypeHandlers;

namespace Nevermore
{
    /// <summary>
    /// While <see cref="ITypeHandler" /> lets you control how a value from the database is mapped to a CLR type (e.g.,
    /// datetime2 -> DateTime), they don't let you control how the value is assigned to a property or field on a mapped
    /// class. A property handler allows you to do that. There shouldn't be many reasons to use them though - mostly
    /// for situations where you don't want to use the default setter (maybe add to a collection instead?)
    /// </summary>
    public interface IPropertyHandler
    {
        bool CanRead { get { return true;  } }
        object Read(object target);
        
        bool CanWrite { get { return true;  } }
        void Write(object target, object value);
    }
}