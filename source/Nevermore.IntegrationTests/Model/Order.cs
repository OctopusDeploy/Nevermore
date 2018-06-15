using System.Collections.Generic;
using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{

    public class Order : IDocument
    {
        public Order()
        {
            
        }

        public Order(IReadOnlyList<string> relatedDocumentIds)
        {
            RelatedDocumentIds = relatedDocumentIds;
        }
        
        public string Id { get; set; }
        public string Name { get; set; }

        public IEnumerable<string> RelatedDocumentIds { get; }
    }
}