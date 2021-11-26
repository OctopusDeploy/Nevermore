using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductSubtotalMap : DocumentMap<ProductSubtotal>
    {
        public ProductSubtotalMap()
        {
            Column(m => m.ProductId);
            Column(m => m.ProductName);
            Column(m => m.Subtotal);
            JsonStorageFormat = JsonStorageFormat.NoJson;
        }
    }
}