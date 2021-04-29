using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithRowVersionMap : DocumentMap<DocumentWithRowVersion>
    {
        public DocumentWithRowVersionMap()
        {
            Id().MaxLength(100);
            Column(m => m.Name).MaxLength(100);
            RowVersion(m => m.RowVersion);
            Unique($"Unique{nameof(DocumentWithRowVersion)}Name", new[] { "Name" }, "Documents must have unique names");
        }
    }
}