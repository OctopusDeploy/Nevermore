using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Legacy.Model
{
    public class LineItemMap : DocumentMap<LineItem>
    {
        public LineItemMap()
        {
            Column(m => m.Name);
            Column(m => m.ProductId);
            Column(m => m.PurchaseDate);
        }
    }
}