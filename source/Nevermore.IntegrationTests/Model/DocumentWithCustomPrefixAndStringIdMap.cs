using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithCustomPrefixAndStringIdMap : DocumentMap<DocumentWithCustomPrefixAndStringId>
    {
        public const string CustomPrefix = "CustomPrefix";

        public DocumentWithCustomPrefixAndStringIdMap()
        {
            Id().KeyHandler(new StringPrimaryKeyHandler(_ => CustomPrefix));
            Column(m => m.Name);
        }
    }
}