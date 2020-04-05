using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class OrderMap : DocumentMap<Order>
    {
        public OrderMap(RelationalStoreConfiguration relationalStoreConfiguration) : base(relationalStoreConfiguration)
        {
            RelatedDocuments(o => o.RelatedDocuments);
        }
    }
}