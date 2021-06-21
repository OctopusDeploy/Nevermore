using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithCustomPrefixMap : DocumentMap<DocumentWithCustomPrefix>
    {
        public const string CustomPrefix = "CustomPrefix";

        public DocumentWithCustomPrefixMap()
        {
            Id().Prefix(_ => CustomPrefix);
            Column(m => m.Name);
        }
    }
}