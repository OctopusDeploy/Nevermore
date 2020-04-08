using System.Reflection;

namespace Nevermore.Mapping
{
    class CustomTypeReaderWriter : IPropertyReaderWriter<object>
    {
        readonly CustomTypeSerialization customTypeSerialization;
        readonly PropertyInfo property;

        public CustomTypeReaderWriter(CustomTypeSerialization customTypeSerialization, PropertyInfo property)
        {
            this.customTypeSerialization = customTypeSerialization;
            this.property = property;
        }

        public object Read(object target)
        {
            var value = property.GetValue(target);
            return customTypeSerialization.ConvertToIndexedColumnDbValue(value);
        }

        public void Write(object target, object value)
        {
            var convertedValue = customTypeSerialization.ConvertFromIndexedColumnDbValue(value, property.PropertyType);
            property.SetValue(target, convertedValue);
        }
    }
}