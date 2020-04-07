using System.Reflection;

namespace Nevermore.Mapping
{
    class CustomTypeReaderWriter : IPropertyReaderWriter<object>
    {
        readonly CustomSingleTypeDefinition customTypeDefinition;
        readonly PropertyInfo property;

        public CustomTypeReaderWriter(CustomSingleTypeDefinition customTypeDefinition, PropertyInfo property)
        {
            this.customTypeDefinition = customTypeDefinition;
            this.property = property;
        }

        public object Read(object target)
        {
            var value = property.GetValue(target);
            return customTypeDefinition.ToDbValue(value, false);
        }

        public void Write(object target, object value)
        {
            var convertedValue = customTypeDefinition.FromDbValue(value, property.PropertyType);
            property.SetValue(target, convertedValue);
        }
    }
}