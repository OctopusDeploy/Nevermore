using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    public interface IInheritedClassSerialization
    {
        JsonConverter GetJsonConverter(RelationalMappings relationalMappings);
    }
}