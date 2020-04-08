using System.Reflection;

namespace Nevermore.Mapping
{
    class CustomTypeReaderWriter : IPropertyReaderWriter<object>
    {
        readonly CustomTypeDefinition customTypeDefinition;
        readonly PropertyInfo property;

        public CustomTypeReaderWriter(CustomTypeDefinition customTypeDefinition, PropertyInfo property)
        {
            this.customTypeDefinition = customTypeDefinition;
            this.property = property;
        }

        public object Read(object target)
        {
            var value = property.GetValue(target);
            return customTypeDefinition.ConvertToIndexedColumnDbValue(value);
        }

        public void Write(object target, object value)
        {
            var convertedValue = customTypeDefinition.ConvertFromIndexedColumnDbValue(value, property.PropertyType);
            property.SetValue(target, convertedValue);
        }
    }
}