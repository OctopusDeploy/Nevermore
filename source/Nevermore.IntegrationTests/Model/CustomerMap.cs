using Nevermore.Contracts;
using Nevermore.Mapping;
using TinyTypes.TypedStrings;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Column(m => m.FirstName).WithMaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname).Nullable();
            Column(m => m.Roles);
            Column(m => m.RowVersion).ReadOnly();
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }

    public class CustomerToTestSerialization : IId<CustomerToTestSerializationId>, IId
    {
        public CustomerToTestSerializationId Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Nickname { get; set; }
        public ReferenceCollection Roles { get; private set; }
        public string JSON { get; set; }
        string IId.Id => Id?.Value;
    }

    public class CustomerToTestSerializationId: TypedString, IIdWrapper
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