using System;
using System.Data.Common;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerMap : DocumentMap<Customer>
    {
        public CustomerMap()
        {
            Id(x => x.Id).MaxLength(100);
            Column(m => m.FirstName).MaxLength(20);
            Column(m => m.LastName).MaxLength(50);
            Column(m => m.Nickname);
            Column(m => m.Roles);
            Unique("UniqueCustomerNames", new[] { "FirstName", "LastName" }, "Customers must have a unique name");
        }
    }

    class CustomerIdTypeHandler : ITypeHandler
    {
        public bool CanConvert(Type objectType)
        {
            return objectType == typeof(CustomerId);
        }

        public object ReadDatabase(DbDataReader reader, int columnIndex)
        {
            if (reader.IsDBNull(columnIndex))
                return default(CustomerId);
            var text = reader.GetString(columnIndex);
            return new CustomerId(text);
        }

        public void WriteDatabase(DbParameter parameter, object value)
        {
            parameter.Value = ((CustomerId)value)?.Value;
        }
    }
}