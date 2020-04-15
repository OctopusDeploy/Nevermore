using System.Data;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class OrderMap : DocumentMap<Order>
    {
        public OrderMap()
        {
            RelatedDocuments(o => o.RelatedDocuments);
        }
    }
}