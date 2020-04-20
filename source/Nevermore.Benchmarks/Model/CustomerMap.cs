using Nevermore.Mapping;

namespace Nevermore.Benchmarks.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Column(m => m.FirstName).MaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Nickname);
            Column(m => m.CreationDate);
            Column(m => m.LastChangeDate);
            Column(m => m.Counter1);
            Column(m => m.Counter2);
            Column(m => m.Counter3);
            Column(m => m.Counter4);
            Column(m => m.Counter5);
            Column(m => m.Counter6);
            Column(m => m.Counter7);
            Column(m => m.Counter8);
            Column(m => m.Counter9);
            Column(m => m.RowVersion).LoadOnly();
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }
}