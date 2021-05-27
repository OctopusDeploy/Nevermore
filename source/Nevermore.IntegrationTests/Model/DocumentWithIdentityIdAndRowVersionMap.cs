using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithIdentityIdAndRowVersionMap : DocumentMap<DocumentWithIdentityIdAndRowVersion>
    {
        public DocumentWithIdentityIdAndRowVersionMap()
        {
            Id(t => t.Id).Identity();
            Column(t => t.Name);
            RowVersion(m => m.RowVersion);
        }
    }
}