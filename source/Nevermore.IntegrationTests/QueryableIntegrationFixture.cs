using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class QueryableIntegrationFixture : FixtureWithRelationalStore
    {
        [Test]
        public void Where()
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
    }
}