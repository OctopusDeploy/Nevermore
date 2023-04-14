using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.Advanced.Queryable;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class QueryableIntegrationFixture : FixtureWithRelationalStore
    {
        [Test]
        public void WhereEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.FirstName == "Alice")
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void WhereEqualIdColumn()
        {
            using var t = Store.BeginTransaction();

            var alice = new Customer { FirstName = "Alice", LastName = "Apple" };
            var bob = new Customer { FirstName = "Bob", LastName = "Banana" };
            var charlie = new Customer { FirstName = "Charlie", LastName = "Cherry" };

            foreach (var c in new[] { alice, bob, charlie })
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Id == alice.Id)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void WhereEqualTypeResolutionColumn()
        {
            using var t = Store.BeginTransaction();

            var testBrands = new Brand[]
            {
                new BrandA { Name = "First Brand" },
                new BrandA { Name = "Another Brand" },
                new BrandB { Name = "Last Brand" }
            };

            foreach (var c in testBrands)
            {
                t.Insert(c);
            }

            t.Commit();

            var brands = t.Queryable<Brand>()
                .Where(b => b.Type == "BrandB")
                .ToList();

            brands.Select(c => c.Name).Should().BeEquivalentTo("Last Brand");
        }

        [Test]
        public void WhereEqualRowVersionColumn()
        {
            using var t = Store.BeginTransaction();

            var testDoc1 = new DocumentWithRowVersion { Name = "First Document" };
            var testDoc2 = new DocumentWithRowVersion { Name = "Second Document" };
            var testDoc3 = new DocumentWithRowVersion { Name = "Third Document" };

            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();
            foreach (var c in new[] { testDoc1, testDoc2, testDoc3 })
            {
                t.Insert(c);
            }

            t.Commit();

            var documents = t.Queryable<DocumentWithRowVersion>()
                .Where(d => d.RowVersion == testDoc3.RowVersion)
                .ToList();

            documents.Select(d => d.Name).Should().BeEquivalentTo("Third Document");
        }

        [Test]
        public async Task WhereEqualPolymorphicDocumentColumn()
        {
            using var t = Store.BeginTransaction();

            var testProducts = new Product[]
            {
                new DodgyProduct { Name = "iPhane", Price = 350.0m, Tax = 35.0m },
                new SpecialProduct { Name = "OctoPhone", Price = 300.0m },
            };

            foreach (var p in testProducts)
            {
                t.Insert(p);
            }

            t.Commit();

            // query by base type
            var dodgyProduct = t.Queryable<Product>()
                .FirstOrDefault(p => p.Name == "iPhane");

            dodgyProduct.Price.Should().Be(350);

            // query by derived type
            var specialProduct = await t
                .Queryable<SpecialProduct>()
                .FirstOrDefaultAsync(p => p.Name == "OctoPhone", CancellationToken.None);

            specialProduct.Price.Should().Be(300);
        }

        [Test]
        public void WhereEqualJson()
        {
            using var t = Store.BeginTransaction();

            var testMachines = new[]
            {
                new Machine { Name = "Machine A", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle A" } },
                new Machine { Name = "Machine B", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle B" } },
                new Machine { Name = "Machine C", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle C" } },
            };

            foreach (var c in testMachines)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Machine>()
                .Where(m => m.Endpoint.Name == "Tentacle A")
                .ToList();

            customers.Select(m => m.Name).Should().BeEquivalentTo("Machine A");
        }

        [Test]
        public void WhereIsNullJsonValue()
        {
            using var t = Store.BeginTransaction();

            var testMachines = new[]
            {
                new Machine { Name = "Machine A", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle A" } },
                new Machine { Name = "Machine B", Endpoint = new PassiveTentacleEndpoint() },
                new Machine { Name = "Machine C", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle C" } },
            };

            foreach (var c in testMachines)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Machine>()
                .Where(m => m.Endpoint.Name == null || m.Endpoint.Name == "Tentacle A")
                .ToList();

            customers.Select(m => m.Name).Should().BeEquivalentTo("Machine A", "Machine B");
        }

        [Test]
        public void WhereIsNullJsonObject()
        {
            using var t = Store.BeginTransaction();

            var testMachines = new[]
            {
                new Machine { Name = "Machine A", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle A" } },
                new Machine { Name = "Machine B", },
                new Machine { Name = "Machine C", Endpoint = new PassiveTentacleEndpoint { Name = "Tentacle C" } },
            };

            foreach (var c in testMachines)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Machine>()
                .Where(m => m.Endpoint == null || m.Endpoint.Name == "Tentacle A")
                .ToList();

            customers.Select(m => m.Name).Should().BeEquivalentTo("Machine A", "Machine B");
        }

        [Test]
        public void WhereNotEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.FirstName != "Alice")
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana", "Cherry");
        }

        [Test]
        public void WhereGreaterThan()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance > 100)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereLessThan()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance < 100)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereGreaterThanOrEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance >= 301.4m)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereLessThanOrEqual()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance <= 56.3m)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereUnaryBool()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", IsVip = false },
                new Customer { FirstName = "Bob", LastName = "Banana", IsVip = true },
                new Customer { FirstName = "Charlie", LastName = "Cherry", IsVip = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.IsVip)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereNotUnaryBool()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", IsVip = false },
                new Customer { FirstName = "Bob", LastName = "Banana", IsVip = true },
                new Customer { FirstName = "Charlie", LastName = "Cherry", IsVip = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => !c.IsVip)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereUnaryBoolJson()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", IsEmployee = false },
                new Customer { FirstName = "Bob", LastName = "Banana", IsEmployee = true },
                new Customer { FirstName = "Charlie", LastName = "Cherry", IsEmployee = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.IsEmployee)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereNotUnaryBoolJson()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", IsEmployee = false },
                new Customer { FirstName = "Bob", LastName = "Banana", IsEmployee = true },
                new Customer { FirstName = "Charlie", LastName = "Cherry", IsEmployee = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => !c.IsEmployee)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }

        [Test]
        public void WhereCompositeAnd()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m, IsEmployee = false },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m, IsEmployee = true },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m, IsEmployee = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance >= 50m && c.IsEmployee && c.FirstName.StartsWith("B"))
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana");
        }

        [Test]
        public void WhereCompositeOr()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m, IsEmployee = true },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m, IsEmployee = false },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m, IsEmployee = false }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Balance < 40m || c.IsEmployee || c.LastName.Contains("n"))
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Banana");
        }

        [Test]
        public void WhereContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = new[] { "Apple", "Orange", "Peach" };
            var customers = t.Queryable<Customer>()
                .Where(c => names.Contains(c.LastName))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice");
        }

        [Test]
        public void WhereContainsEmpty()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = Array.Empty<string>();
            var customers = t.Queryable<Customer>()
                .Where(c => names.Contains(c.LastName))
                .ToList();

            customers.Should().BeEmpty();
        }

        [Test]
        public void WhereContainsOnDocument()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Roles = { "RoleA", "RoleB" } },
                new Customer { FirstName = "Bob", LastName = "Banana", Roles = { "RoleA", "RoleC" } },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Roles = { "RoleB", "RoleC" } }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.Roles.Contains("RoleC"))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
        }

        [Test]
        public void WhereContainsOnDocumentJson()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 78, 321 } },
                new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 662, 91 } },
                new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 4, 18 } }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.LuckyNumbers.Contains(4))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Charlie");
        }

        [Test]
        public void WhereNotContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = new[] { "Apple", "Orange", "Peach" };
            var customers = t.Queryable<Customer>()
                .Where(c => !names.Contains(c.LastName))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
        }

        [Test]
        public void WhereNotContainsEmpty()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Balance = 987.4m },
                new Customer { FirstName = "Bob", LastName = "Banana", Balance = 56.3m },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Balance = 301.4m }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var names = Array.Empty<string>();
            var customers = t.Queryable<Customer>()
                .Where(c => !names.Contains(c.LastName))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice", "Bob", "Charlie");
        }

        [Test]
        public void WhereNotStringContains()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bear" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Beets" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Chicken" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => !c.Nickname.Contains("hi"))
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice", "Bob");
        }

        [Test]
        public void First()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .First();

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .First(c => c.FirstName == "Alice");

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstOrDefault()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .FirstOrDefault();

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public async Task FirstOrDefaultAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var customer = await t.Queryable<Customer>()
                .FirstOrDefaultAsync();

            customer.LastName.Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstOrDefaultWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .FirstOrDefault(c => c.FirstName.EndsWith("y"));

            customer.Should().BeNull();
        }

        [Test]
        public async Task FirstOrDefaultWithPredicateAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var customer = await t.Queryable<Customer>()
                .FirstOrDefaultAsync(c => c.FirstName.EndsWith("y"));

            customer.Should().BeNull();
        }

        [Test]
        public void Skip()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Skip(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry");
        }

        [Test]
        public void Take()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Take(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Banana");
        }

        [Test]
        public void SkipAndTake()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" },
                new Customer { FirstName = "Dan", LastName = "Durian" },
                new Customer { FirstName = "Erin", LastName = "Eggplant" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Skip(2)
                .Take(2)
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry", "Durian");
        }

        [Test]
        public void Count()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var count = t.Queryable<Customer>().Count();

            count.Should().Be(3);
        }

        [Test]
        public async Task CountAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var count = await t.Queryable<Customer>().CountAsync();

            count.Should().Be(3);
        }

        [Test]
        public async Task CountAsyncPolymorphic()
        {
            using var t = Store.BeginTransaction();

            var testBrands = new Brand[]
            {
                new BrandA { Name = "Brand 1" },
                new BrandB { Name = "Brand 2" },
                new BrandA { Name = "Brand 3" }
            };

            foreach (var b in testBrands)
            {
                t.Insert(b);
            }

            await t.CommitAsync();

            var count = await t.Queryable<BrandB>().CountAsync();

            count.Should().Be(1);
        }

        [Test]
        public void Hint()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var a = t.Queryable<Customer>().Where(x => x.FirstName == "Alice").Hint("WITH (ROWLOCK, UPDLOCK, NOWAIT)").RawDebugView();
            a.Should().Be($"SELECT Id,FirstName,LastName,Nickname,Roles,Balance,IsVip,JSON{Environment.NewLine}" +
                          $"FROM [TestSchema].[Customer] WITH (ROWLOCK, UPDLOCK, NOWAIT){Environment.NewLine}" +
                          "WHERE ([FirstName] = @p1)");
        }

        [Test]
        public void CountWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bandit" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Chief" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Cherry Bomb" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var count = t.Queryable<Customer>().Count(c => c.Nickname.StartsWith("C"));

            count.Should().Be(2);
        }

        [Test]
        public async Task CountWithPredicateAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bandit" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Chief" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Cherry Bomb" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var count = await t.Queryable<Customer>().CountAsync(c => c.Nickname.StartsWith("C"));

            count.Should().Be(2);
        }

        [Test]
        public async Task CountWithPredicateAsyncPolymorphic()
        {
            using var t = Store.BeginTransaction();

            var testBrands = new Brand[]
            {
                new BrandA { Name = "Brand 1" },
                new BrandB { Name = "Brand 2" },
                new BrandA { Name = "Brand 3" }
            };

            foreach (var b in testBrands)
            {
                t.Insert(b);
            }

            await t.CommitAsync();

            var count = await t.Queryable<BrandA>().CountAsync(b => b.Name.StartsWith("Brand"));

            count.Should().Be(2);
        }

        [Test]
        public void CountWithOrderBy()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Bandit" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Chief" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Cherry Bomb" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var count = t.Queryable<Customer>().OrderBy(c => c.LastName).Count(c => c.Nickname.StartsWith("C"));

            count.Should().Be(2);
        }

        [Test]
        public void OrderBy()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Zeta" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Omega" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>().OrderBy(c => c.Nickname).ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana", "Cherry", "Apple");
        }

        [Test]
        public void OrderByDescending()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>().OrderByDescending(c => c.Nickname).ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Cherry", "Apple", "Banana");
        }

        [Test]
        public void OrderByThenBy()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Beta" },
                new Customer { FirstName = "Amanda", LastName = "Apple", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Omega" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.Nickname)
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Amanda", "Alice", "Charlie");
        }

        [Test]
        public void OrderByThenByDescending()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Beta" },
                new Customer { FirstName = "Amanda", LastName = "Apple", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Omega" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .OrderBy(c => c.LastName)
                .ThenByDescending(c => c.Nickname)
                .ToList();

            customers.Select(c => c.FirstName).Should().BeEquivalentTo("Alice", "Amanda", "Charlie");
        }

        [Test]
        public void Any()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var anyCustomers = t.Queryable<Customer>().Any();

            anyCustomers.Should().BeTrue();
        }

        [Test]
        public async Task AnyAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var anyCustomers = await t.Queryable<Customer>().AnyAsync();

            anyCustomers.Should().BeTrue();
        }

        [Test]
        public void AnyWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var anyCustomers = t.Queryable<Customer>().Any(c => c.Nickname == "Warlock");

            anyCustomers.Should().BeFalse();
        }

        [Test]
        public async Task AnyWithPredicateAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Nickname = "Omega" },
                new Customer { FirstName = "Bob", LastName = "Banana", Nickname = "Alpha" },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Nickname = "Zeta" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var anyCustomers = await t.Queryable<Customer>().AnyAsync(c => c.Nickname == "Warlock");

            anyCustomers.Should().BeFalse();
        }

        [Test]
        public void WhereCustomSql()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Roles = { "Admin" } },
                new Customer { FirstName = "Bob", LastName = "Banana", Roles = { "Boss" } },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Roles = { "Editor", "Bum" } }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>().WhereCustom("[Roles] LIKE '%|B%'").ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Banana", "Cherry");
        }

        [Test]
        public async Task ToListAsync()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Roles = { "Admin" } },
                new Customer { FirstName = "Bob", LastName = "Banana", Roles = { "Boss" } },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Roles = { "Editor", "Bum" } }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            await t.CommitAsync();

            var customers = await t.Queryable<Customer>().Where(c => c.FirstName == "Alice").ToListAsync();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple");
        }

        [Test]
        public async Task AsyncForeach()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple", Roles = { "Admin" } },
                new Customer { FirstName = "Bob", LastName = "Banana", Roles = { "Boss" } },
                new Customer { FirstName = "Charlie", LastName = "Cherry", Roles = { "Editor", "Bum" } }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            var customers = new List<Customer>();
            var queryable = (INevermoreQueryable<Customer>)t.Queryable<Customer>().Where(c => c.FirstName.EndsWith("e"));
            await foreach (var customer in queryable)
            {
                customers.Add(customer);
            }

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple", "Cherry");
        }
    }
}