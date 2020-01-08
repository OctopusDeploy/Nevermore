using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Legacy.Model
{
    public class ProductSubtotalMap : DocumentMap<ProductSubtotal>
    {
        public ProductSubtotalMap()
        {
            Column(m => m.Id);
            Column(m => m.ProductId);
            Column(m => m.ProductName);
            Column(m => m.Subtotal);
        }
    }
}