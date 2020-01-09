using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Contracts;
using TinyTypes.TypedStrings;

namespace Nevermore.IntegrationTests.Model
{

    public class Order : IDocument<OrderId>, IId
    {
        public Order()
        {
            
        }

        public Order(IEnumerable<(string, Type)> relatedDocuments)
        {
            RelatedDocuments = relatedDocuments?.ToArray();
        }
        
        public OrderId Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<(string, Type)> RelatedDocuments { get; }
        string IId.Id => Id?.Value;
    }

    public class OrderId : TypedString, IIdWrapper
    {
        public OrderId(string value) : base(value)
        {
        }
    }
}