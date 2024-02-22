using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced;

public class CustomKeyAllocatorFixture : FixtureWithDatabase
{
    readonly RelationalStore store;

    class SimpleDocument
    {
        public string Id { get; private set; }
        public string Name { get; set; }
    }

    class SimpleDocumentMap : DocumentMap<SimpleDocument>
    {
        public SimpleDocumentMap()
        {
            Id().KeyHandler(new StringPrimaryKeyHandler("Simples"));
            Column(m => m.Name).MaxLength(20);
        }
    }

    public CustomKeyAllocatorFixture()
    {
        var config = new RelationalStoreConfiguration(ConnectionString)
        {
            KeyAllocatorFactory = () => new CustomKeyAllocator(),
        };
        config.DocumentMaps.Register(new SimpleDocumentMap());

        store = new RelationalStore(config);

        ExecuteSql(@"
                CREATE TABLE [SimpleDocument] (
                  [Id] NVARCHAR(50) NOT NULL CONSTRAINT [PK__Id] PRIMARY KEY CLUSTERED,
                  [Name] NVARCHAR(20) NOT NULL,
                  [JSON] NVARCHAR(MAX) NOT NULL
                )
                ");
    }

    [SetUp]
    public void SetUp()
    {
        // Reset the allocated Ids so we get a predictable result
        store.Reset();
    }

    [Test, Order(1)]
    public void ShouldAllocateCustomKeys()
    {
        using var transaction = store.BeginTransaction();

        var allocated1 = transaction.AllocateId("Foo", "Bars");
        var allocated2 = transaction.AllocateId("Foo", "Bars");
        var allocated3 = transaction.AllocateId("Foo", "Bars");

        allocated1.Should().Be("Bars-100");
        allocated2.Should().Be("Bars-200");
        allocated3.Should().Be("Bars-300");
    }

    [Test, Order(2)]
    public void ShouldAllocateCustomKeysByMapping()
    {
        using var transaction = store.BeginTransaction();

        var allocated1 = transaction.AllocateId<string>(typeof(SimpleDocument));
        var allocated2 = transaction.AllocateId<string>(typeof(SimpleDocument));
        var allocated3 = transaction.AllocateId<string>(typeof(SimpleDocument));

        allocated1.Should().Be("Simples-100");
        allocated2.Should().Be("Simples-200");
        allocated3.Should().Be("Simples-300");
    }

    [Test, Order(3)]
    public void ShouldAllocateCustomKeyToInsertedDocuments()
    {
        var document = new SimpleDocument() { Name = "Donald" };

        using var transaction = store.BeginTransaction();

        transaction.Insert(document);
        transaction.TryCommit();

        document.Id.Should().Be("Simples-100");
    }

    // This is a simple in-memory custom key allocator that allocates keys in intervals of 100 without relying on the database.
    class CustomKeyAllocator : IKeyAllocator
    {
        readonly ConcurrentDictionary<string, int> allocations = new(StringComparer.OrdinalIgnoreCase);

        public void Reset()
        {
            allocations.Clear();
        }

        public int NextId(string tableName)
        {
            return allocations.AddOrUpdate(tableName, (_) => 100, (_, prev) => prev + 100);
        }

        public ValueTask<int> NextIdAsync(string tableName, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(NextId(tableName));
        }
    }
}