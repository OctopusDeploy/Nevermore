using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class Product : IDocument
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public ProductType Type { get; set; } = ProductType.Normal;
    }

    public enum ProductType
    {
        Normal,
        Special,
        Dodgy
    }


    public class SpecialProduct : Product
    {
        public SpecialProduct()
        {
            Type = ProductType.Special;
        }

        public string BonusMaterial { get; set; }
    }

    public class DodgyProduct : Product
    {
        public DodgyProduct()
        {
            Type = ProductType.Dodgy;
        }

        public decimal Tax { get; set; }
    }
}