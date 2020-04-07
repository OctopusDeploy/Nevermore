using System;
using System.Collections.Generic;
using System.Data;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;

namespace Nevermore.IntegrationTests.CustomTypes
{
    class CustomTypeUsedAsAPropertyCustomTypeDefinition : CustomSingleTypeDefinition
    {
        public override Type TypeToConvert => typeof(CustomTypeUsedAsAProperty);
        public override DbType DbType => DbType.String;
        public override int MaxLength => 50;

        public override object ToDbValue(object instance, bool isJson)
        {
            return JsonConvert.SerializeObject(instance);
        }
        public override object FromDbValue(object value, Type targetType)
        {
            return JsonConvert.DeserializeObject<CustomTypeUsedAsAProperty>((string)value);
        }
    }
}