using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Id().MaxLength(100);
            Column(m => m.FirstName).MaxLength(20);
            Column(m => m.LastName).MaxLength(50);
            Column(m => m.Nickname);
            Column(m => m.Roles);
            Column(m => m.RowVersion).LoadOnly();
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }
}