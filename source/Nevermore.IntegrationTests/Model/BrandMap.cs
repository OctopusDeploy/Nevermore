using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class BrandMap : DocumentMap<Brand>
    {
        public BrandMap()
        {
            Column(m => m.Name);
            TypeResolutionColumn(m => m.Type).SaveOnly();
        }
    }
}