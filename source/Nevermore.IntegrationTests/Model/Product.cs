namespace Nevermore.IntegrationTests.Model
{
    public class Product
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public ProductType Type { get; set; } = ProductType.Normal;
    }
}