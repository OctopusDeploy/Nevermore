using System.Reflection;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    public class CustomTypeReaderWriter : IPropertyReaderWriter<object>
    {
        readonly ICustomTypeDefinition customTypeDefinition;
        readonly PropertyInfo property;

        public CustomTypeReaderWriter(ICustomTypeDefinition customTypeDefinition, PropertyInfo property)
        {
            this.customTypeDefinition = customTypeDefinition;
            this.property = property;
        }

        public object Read(object target)
        {
            var value = property.GetValue(target);
            return customTypeDefinition.ToDbValue(value);
        }

        public void Write(object target, object value)
        {
            var convertedValue = customTypeDefinition.FromDbValue(value);
            property.SetValue(target, convertedValue);
        }
    }
}