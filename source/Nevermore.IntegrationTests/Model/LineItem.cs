using System;
using Nevermore.Contracts;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class LineItem : IDocument<LineItemId>, IId
    {
        public LineItemId Id { get; private set; }
        public string Name { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string ProductId { get; set; }
        public Quantity Quantity { get; set; }
        string IId.Id => Id?.Value;
    }

    public class Quantity : TypedInt
    {
        public Quantity(int value) : base(value)
        {
        }
    }

    public class LineItemId : CaseSensitiveTypedString, IIdWrapper
    {
        public LineItemId(string value) : base(value)
        {
        }
    }
}