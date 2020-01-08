using System;
using Nevermore.Contracts;
using Nevermore.TypedStrings;

namespace Nevermore.IntegrationTests.Model
{
    public class LineItem : IDocument<LineItemId>, IId
    {
        public LineItemId Id { get; private set; }
        public string Name { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        string IId.Id => Id?.Value;
    }

    public class LineItemId : TypedString, IIdWrapper
    {
        public LineItemId(string value) : base(value)
        {
        }
    }
}