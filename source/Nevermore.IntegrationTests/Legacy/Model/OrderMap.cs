using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Legacy.Model
{
    public class OrderMap : DocumentMap<Order>
    {
        public OrderMap()
        {
            RelatedDocuments(o => o.RelatedDocuments);
        }
    }
}