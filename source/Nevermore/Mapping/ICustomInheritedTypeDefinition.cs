using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    public interface ICustomInheritedTypeDefinition : ICustomTypeDefinition
    {
        JsonConverter GetJsonConverter(RelationalMappings relationalMappings);
    }
}