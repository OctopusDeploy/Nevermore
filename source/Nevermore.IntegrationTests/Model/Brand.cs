using Nevermore.Contracts;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public abstract class Brand : IDocument
    {
        public BrandId Id { get; protected set; }
        public string Name { get; set; }

        public abstract string Type { get; }

        public string Description { get; set; }
     }

    public class BrandId : CaseSensitiveTypedString
    {
        public BrandId(string value) : base(value)
        {
        }
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