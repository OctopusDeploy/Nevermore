using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductSubtotalMap : DocumentMap<ProductSubtotal>
    {
        public ProductSubtotalMap(RelationalStoreConfiguration relationalStoreConfiguration) : base(relationalStoreConfiguration)
        {
            Column(m => m.Id);
            Column(m => m.ProductId);
            Column(m => m.ProductName);
            Column(m => m.Subtotal);
        }
    }
}