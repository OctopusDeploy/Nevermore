using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Legacy.Model
{
    public class ProductSubtotal : IId
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Subtotal { get; set; }
    }
}