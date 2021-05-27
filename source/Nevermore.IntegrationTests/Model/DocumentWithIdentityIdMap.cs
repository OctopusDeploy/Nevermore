using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithIdentityIdMap : DocumentMap<DocumentWithIdentityId>
    {
        public DocumentWithIdentityIdMap()
        {
            Id(t => t.Id).Identity();
            Column(t => t.Name);
        }
    }
}