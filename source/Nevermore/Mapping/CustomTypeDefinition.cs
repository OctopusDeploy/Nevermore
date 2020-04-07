using System;

namespace Nevermore.Mapping
{
    public abstract class CustomTypeDefinition : CustomTypeDefinitionBase
    {
        public abstract object ToDbValue(object instance, bool isJson);
        public abstract object FromDbValue(object value, Type targetType);
    }
}