using Newtonsoft.Json;

namespace Nevermore.Mapping
{
    public interface ITypeDesignatingTypeDefinition
    {
        JsonConverter GetJsonConverter(RelationalMappings relationalMappings);
    }
}