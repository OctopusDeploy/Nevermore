namespace Nevermore.IntegrationTests.Model
{
    public class SpecialProduct : Product
    {
        public SpecialProduct()
        {
            Type = ProductType.Special;
        }

        public string BonusMaterial { get; set; }
    }
}