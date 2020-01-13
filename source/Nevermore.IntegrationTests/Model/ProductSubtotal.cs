using Nevermore.Contracts;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductSubtotal : IId<ProductSubtotalId>, IId
    {
        public ProductSubtotalId Id { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Subtotal { get; set; }
        string IId.Id => Id?.Value;
    }

    public class ProductSubtotalId : CaseSensitiveTypedString, IIdWrapper
    {
        public ProductSubtotalId(string value) : base(value)
        {
        }
    }
}