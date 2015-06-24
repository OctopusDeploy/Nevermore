using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Nevermore.IntegrationTests
{
    public class RelationalStoreFixture : FixtureWithRelationalStore
    {
        public RelationalStoreFixture()
        {
            var mappings = new DocumentMap[]
            {
                new CustomerMap(),
                new ProductMap(),
                new LineItemMap(),
            };

            Mappings.Install(mappings);

            var output = new StringBuilder();
            using (var transaction = Store.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                output.Clear();

                foreach (var map in mappings)
                    SchemaGenerator.WriteTableSchema(map, null, output);

                transaction.ExecuteScalar<int>(output.ToString());

                transaction.Commit();
            }
        }

        [Fact]
        public void ShouldGenerateIdsUnlessExplicitlyAssigned()
        {
            // The K and Id columns allow you to give records an ID, or use an auto-generated, unique ID
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer2 = new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 12, 13 }, Nickname = "B-man", Roles = { "web-server", "app-server" } };
                var customer3 = new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 12, 13 }, Nickname = "Chazza", Roles = { "web-server", "app-server" } };
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3, "Customers-Chazza");

                Assert.StartsWith("Customers-", customer1.Id);
                Assert.StartsWith("Customers-", customer2.Id);
                Assert.Equal("Customers-Chazza", customer3.Id);

                transaction.Commit();
            }
        }
    }

    public class Customer
    {
        public Customer()
        {
            Roles = new HashSet<string>();
        }

        public string Id { get; private set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public HashSet<string> Roles { get; private set; }
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
    }

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

    public class LineItem
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class LineItemMap : DocumentMap<LineItem>
    {
        public LineItemMap()
        {
            Column(m => m.Name);
            Column(m => m.ProductId);
        }
    }

    public class Product
    {
        public string Id { get; private set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }

    public class ProductMap : DocumentMap<Product>
    {
        public ProductMap()
        {
            Column(m => m.Name);
        }
    }

    public class HashSetReaderWriter : PropertyReaderWriterDecorator
    {
        public HashSetReaderWriter(IPropertyReaderWriter<object> original)
            : base(original)
        {
        }

        public override object Read(object target)
        {
            var value = base.Read(target) as HashSet<string>;
            if (value == null || value.Count == 0)
                return "";

            var items = new StringBuilder();
            items.Append("|");
            foreach (var item in value)
            {
                items.Append(item);
                items.Append("|");
            }
            return items.ToString();
        }

        public override void Write(object target, object value)
        {
            var valueAsString = (value ?? string.Empty).ToString().Split('|');

            var collection = base.Read(target) as HashSet<string>;
            if (collection == null)
            {
                base.Write(target, collection = new HashSet<string>());
            }

            collection.ReplaceAll(valueAsString.Where(v => !string.IsNullOrWhiteSpace(v)));
        }

    }

    public static class HashSetExtensions
    {
        public static void ReplaceAll(this HashSet<string> collection, IEnumerable<string> newItems)
        {
            collection.Clear();

            if (newItems == null) return;

            foreach (var item in newItems)
            {
                collection.Add(item);
            }
        }
    }

}
