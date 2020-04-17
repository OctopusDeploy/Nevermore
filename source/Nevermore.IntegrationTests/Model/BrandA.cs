namespace Nevermore.IntegrationTests.Model
{
    public class BrandA : Brand
    {
        public const string BrandType = "BrandA";
        public override string Type => BrandType;
    }
}