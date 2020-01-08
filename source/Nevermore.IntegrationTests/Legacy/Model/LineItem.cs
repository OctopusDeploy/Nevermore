using System;
using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Legacy.Model
{
    public class LineItem : IDocument
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }
}