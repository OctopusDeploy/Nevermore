using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{

    public class Order : IDocument
    {
        public Order()
        {
            
        }

        public Order(IEnumerable<(string, Type)> relatedDocuments)
        {
            RelatedDocuments = relatedDocuments?.ToArray();
        }
        
        public string Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<(string, Type)> RelatedDocuments { get; }
    }
}