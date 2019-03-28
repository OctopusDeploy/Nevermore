using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductSubtotal
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Subtotal { get; set; }
    }
}