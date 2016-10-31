namespace Nevermore.IntegrationTests.Model
{
    public class Product : IDocument
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}