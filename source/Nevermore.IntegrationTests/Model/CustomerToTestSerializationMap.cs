using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerToTestSerializationMap : DocumentMap<CustomerToTestSerialization>
    {
        public CustomerToTestSerializationMap()
        {
            TableName = "Customer";

            Column(m => m.FirstName).MaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname).Nullable();
            Column(m => m.Roles);
            Column(m => m.JSON);
        }
    }
}