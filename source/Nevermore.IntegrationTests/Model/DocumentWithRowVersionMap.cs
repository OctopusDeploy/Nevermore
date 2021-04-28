using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithRowVersionMap : DocumentMap<DocumentWithRowVersion>
    {
        public DocumentWithRowVersionMap()
        {
            Id().MaxLength(100);
            RowVersion(m => m.RowVersion);
        }
    }
}