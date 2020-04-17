namespace Nevermore.IntegrationTests.Model
{
    public class DodgyProduct : Product
    {
        public DodgyProduct()
        {
            Type = ProductType.Dodgy;
        }

        public decimal Tax { get; set; }
    }
}