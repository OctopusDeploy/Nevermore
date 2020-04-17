using Nevermore.Mapping;

namespace Nevermore.Benchmarks.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Column(m => m.FirstName).MaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname).Nullable();
            Column(m => m.RowVersion).LoadOnly();
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }
}