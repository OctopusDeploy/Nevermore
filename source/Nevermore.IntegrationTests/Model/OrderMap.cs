using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class OrderMap : DocumentMap<Order>
    {
        public OrderMap()
        {
            TypedIdColumn(o => o.Id);
            RelatedDocuments(o => o.RelatedDocuments);
        }
    }
}