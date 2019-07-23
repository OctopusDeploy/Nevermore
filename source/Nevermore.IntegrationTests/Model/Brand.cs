using Nevermore.Contracts;
using Newtonsoft.Json;

namespace Nevermore.IntegrationTests.Model
{
    public abstract class Brand : IDocument
    {
        public string Id { get; protected set; }
        public string Name { get; set; }

        public abstract string Type { get; }

        public string Description { get; set; }
    }

    public class BrandA : Brand
    {
        public const string BrandType = "BrandA";
        public override string Type => BrandType;
    }

    public class BrandB : Brand
    {
        public const string BrandType = "BrandB";
        public override string Type => BrandType;
    }
}