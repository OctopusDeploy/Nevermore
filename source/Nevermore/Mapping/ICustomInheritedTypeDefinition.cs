using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    public interface ICustomInheritedTypeDefinition
    {
        JsonConverter GetJsonConverter(RelationalMappings relationalMappings);
    }
}