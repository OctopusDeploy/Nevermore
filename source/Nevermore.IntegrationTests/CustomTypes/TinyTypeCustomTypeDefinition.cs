using System;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.CustomTypes
{
    class TinyTypeCustomTypeDefinition : CustomTypeDefinition
    {
        public override bool CanConvertType(Type type)
        {
            return typeof(TinyType<string>).IsAssignableFrom(type);
        }

        public override object ConvertToJsonColumnValue(object instance)
        {
            return ((TinyType<string>) instance).Value;
        }

        public override object ConvertToIndexedColumnDbValue(object instance)
        {
            return ConvertToJsonColumnValue(instance);
        }

        public override object ConvertFromJsonDbValue(object value, Type targetType)
        {
            var tinyType = Activator.CreateInstance(targetType, value);
            return tinyType;
        }

        public override object ConvertFromIndexedColumnDbValue(object value, Type targetType)
        {
            return ConvertFromJsonDbValue(value, targetType);
        }
    }
    
    class TinyType<T>
    {
        public TinyType(T value)
        {
            Value = value;
        }

        public T Value { get; }
    }

    class ProjectId : TinyType<string>
    {
        public ProjectId(string value) : base(value)
        {
        }
    }
}