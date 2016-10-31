using System.Data;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Column(m => m.FirstName).WithMaxLength(20);
            Column(m => m.LastName);
            Column(m => m.Roles, map =>
            {
                map.ReaderWriter = new HashSetReaderWriter(map.ReaderWriter);
                map.DbType = DbType.String;
                map.MaxLength = int.MaxValue;
            });

            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }
}