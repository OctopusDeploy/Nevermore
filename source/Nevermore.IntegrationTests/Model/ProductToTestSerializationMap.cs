using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class ProductToTestSerializationMap : DocumentMap<ProductToTestSerialization>
    {
        public ProductToTestSerializationMap()
        {
            TableName = "Product";
            Column(x => x.Name);
            Column(x => x.Type);
            Column(x => x.JSON);
        }
    }
}