using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductMap<TProduct> : DocumentMap<TProduct> where TProduct : Product
    {
        public ProductMap()
        {
            Column(m => m.Name);
            Column(m => m.Type);
        }
    }
}