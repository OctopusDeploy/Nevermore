using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductToTestSerialization : IDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
     
        public string Type { get; set; }
        public string JSON { get; set; }
    }
}