using System;
using System.Data;
using System.Linq;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.CustomTypes
{
    class VersionCustomTypeSerialization : CustomTypeSerialization
    {
        public override DbType DbType => DbType.String;
        public override int MaxLength => 50;

        public override bool CanConvertType(Type type)
        {
            return type == typeof(Version);
        }

        public override object ConvertToJsonColumnValue(object instance)
        {
            var version = (Version)instance;
            return $"{version.Major}.{version.Minor}.{version.Patch}";
        }
        public override object ConvertToIndexedColumnDbValue(object instance)
        {
            return ConvertToJsonColumnValue(instance);
        }

        public override object ConvertFromJsonDbValue(object value, Type targetType)
        {
            var versionStrings = ((string)value).Split(".").Select(x => Convert.ToInt32(x)).ToArray();
            return new Version(versionStrings[0], versionStrings[1], versionStrings[2]);
        }
        public override object ConvertFromIndexedColumnDbValue(object value, Type targetType)
        {
            return ConvertFromJsonDbValue(value, targetType);
        }
    }
}