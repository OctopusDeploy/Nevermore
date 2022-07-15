using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using Nevermore.TableColumnNameResolvers;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class ReadTransactionFixture : FixtureWithDatabase
    {
        const string DefaultSchemaName = "TestSchema";
        const string DifferentSchemaName = "DifferentSchema";

        IRelationalStoreConfiguration configuration;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            configuration = new RelationalStoreConfiguration(ConnectionString)
            {
                CommandFactory = new SqlCommandFactory(),
                ApplicationName = "Nevermore-IntegrationTests",
                DefaultSchema = DefaultSchemaName
            };
            configuration.DocumentMaps.Register(new TableDocumentMap());

            ExecuteSql($"create table {DefaultSchemaName}.{nameof(TableDocument)} (Id nvarchar(50), ColumnName nvarchar(10))");

            ExecuteSql($"create schema {DifferentSchemaName}");
            ExecuteSql($"create table {DifferentSchemaName}.{nameof(TableDocument)} (ColumnInOtherSchema nvarchar(10))");
        }

        [Test]
        public async Task LoadRequired_WhenNoSchemaSpecifiedThenResolveTableColumnsFromDefaultSchema()
        {
            await AssertSchemaIsCorrectAfterMethodCall(async t =>
            {
                await Task.CompletedTask;
                return t.LoadRequired<TableDocument, string>("MyId");
            });
        }

        [Test]
        public async Task LoadRequiredAsync_WhenNoSchemaSpecifiedThenResolveTableColumnsFromDefaultSchema()
        {
            await AssertSchemaIsCorrectAfterMethodCall(async t => await t.LoadRequiredAsync<TableDocument, string>("MyId"));
        }

        [Test]
        public async Task LoadStream_WhenNoSchemaSpecifiedThenResolveTableColumnsFromDefaultSchema()
        {
            await AssertSchemaIsCorrectAfterMethodCall(async t =>
            {
                await Task.CompletedTask;
                return t.LoadStream<TableDocument, string>("MyId", "TheirId").GetEnumerator().MoveNext();
            });
        }

        [Test]
        public async Task LoadStreamAsync_WhenNoSchemaSpecifiedThenResolveTableColumnsFromDefaultSchema()
        {
            await AssertSchemaIsCorrectAfterMethodCall(async t => await t.LoadStreamAsync<TableDocument, string>(new[] { "MyId", "TheirId" }).GetAsyncEnumerator().MoveNextAsync());
        }

        async Task AssertSchemaIsCorrectAfterMethodCall<T>(Func<IRelationalTransaction, Task<T>> func)
        {
            var store = new RelationalStore(configuration);

            var recordingColumnResolver = new TableColumnNameResolverThatRecordsTheSchema();
            configuration.TableColumnNameResolver = _ => recordingColumnResolver;
            using var transaction = store.BeginTransaction();
            try {
                await func(transaction);
            }
            catch{} //We don't care about any exceptions here (but we're going to have some for resources not found)

            recordingColumnResolver.SchemaNameAsProvided.Should().BeEquivalentTo(configuration.DefaultSchema);
        }

        class TableColumnNameResolverThatRecordsTheSchema : ITableColumnNameResolver
        {
            public string SchemaNameAsProvided;

            public string[] GetColumnNames(string schemaName, string tableName)
            {
                SchemaNameAsProvided = schemaName;
                return new[] { "Id", "ColumnName" };
            }
        }

        class TableDocument
        {
            public string Id { get; set; }
            public string ColumnName { get; set; }
        }

        class TableDocumentMap : DocumentMap<TableDocument>
        {
            public TableDocumentMap()
            {
                Id(x => x.Id);
                Column(x => x.ColumnName);
                JsonStorageFormat = JsonStorageFormat.NoJson;
            }
        }
    }
}