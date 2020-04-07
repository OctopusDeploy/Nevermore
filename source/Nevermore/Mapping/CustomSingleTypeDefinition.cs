using System;

namespace Nevermore.Mapping
{
    public abstract class CustomSingleTypeDefinition : CustomTypeDefinition
    {
        public abstract object ToDbValue(object instance, bool isJson);
        public abstract object FromDbValue(object value, Type targetType);
    }
}