using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithCustomPrefixMap : DocumentMap<DocumentWithCustomPrefix>
    {
        public DocumentWithCustomPrefixMap()
        {
            Id();
            Column(m => m.Name);
        }
    }
}