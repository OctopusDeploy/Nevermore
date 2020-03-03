using Nevermore.Contracts;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductSubtotal : IId
    {
        public ProductSubtotalId Id { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class ProductSubtotalId : CaseSensitiveTypedString
    {
        public ProductSubtotalId(string value) : base(value)
        {
        }
    }
}