using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nevermore.IntegrationTests.Model
{
    public class Order
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public (string, Type)[] SerializedRelatedDocuments { get; set; }

        //RelatedDocuments normally just returns other properties, hence the JsonIgnore
        [JsonIgnore]
        public IEnumerable<(string, Type)> RelatedDocuments => SerializedRelatedDocuments;
    }
}