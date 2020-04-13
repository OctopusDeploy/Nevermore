using Nevermore.Mapping;

namespace Nevermore.Benchmarks.Model
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
}