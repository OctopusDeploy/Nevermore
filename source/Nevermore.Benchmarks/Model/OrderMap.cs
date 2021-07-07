using Nevermore.Mapping;

namespace Nevermore.Benchmarks.Model
{
    public class OrderMap : DocumentMap<Order>
    {
        public OrderMap()
        {
            RelatedDocuments(o => o.RelatedDocuments);
        }
    }
}