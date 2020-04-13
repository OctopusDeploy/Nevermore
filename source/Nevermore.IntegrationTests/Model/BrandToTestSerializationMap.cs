using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class BrandToTestSerializationMap : DocumentMap<BrandToTestSerialization>
    {
        public BrandToTestSerializationMap()
        {
            TableName = "Brand";
            Column(x => x.Name);
            Column(x => x.JSON);
        }
    }
}