using Nevermore.Contracts;
using Nevermore.Mapping;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            TypedIdColumn(m => m.Id);
            Column(m => m.FirstName).WithMaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname).Nullable();
            Column(m => m.Roles);
            Column(m => m.RowVersion).ReadOnly();
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }

    public class CustomerToTestSerialization : IId
    {
        public CustomerToTestSerializationId Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public Nickname Nickname { get; set; }
        public ReferenceCollection Roles { get; private set; }
        public string JSON { get; set; }
    }

    public class CustomerToTestSerializationId: CaseSensitiveTypedString
    {
        public CustomerToTestSerializationId(string value) : base(value)
        {
        }
    }

    public class CustomerToTestSerializationMap : DocumentMap<CustomerToTestSerialization>
    {
        public CustomerToTestSerializationMap()
        {
            TableName = "Customer";

            Column(m => m.FirstName).WithMaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname).Nullable();
            Column(m => m.Roles);
            Column(m => m.JSON);
        }
    }
}