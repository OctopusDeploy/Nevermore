using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class LineItemMap : DocumentMap<LineItem>
    {
        public LineItemMap(RelationalStoreConfiguration relationalStoreConfiguration) : base(relationalStoreConfiguration)
        {
            Column(m => m.Name);
            Column(m => m.ProductId);
            Column(m => m.PurchaseDate);
        }
    }
}