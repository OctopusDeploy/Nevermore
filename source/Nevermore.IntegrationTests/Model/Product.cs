using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductId : TypedString, IIdWrapper
    {
        public ProductId(string value) : base(value)
        {
        }
    }

    public class NotProductId : TypedString
    {
        public NotProductId(string value) : base(value)
        {
        }
    }

    public class Product : IDocument<ProductId>, IDocument
    {
        public ProductId Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }

        public ProductType Type { get; set; } = ProductType.Normal;

        string IId.Id => Id?.Value;
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