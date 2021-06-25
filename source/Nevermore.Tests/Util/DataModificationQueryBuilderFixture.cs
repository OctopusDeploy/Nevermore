using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assent;
using Nevermore.Advanced;
using Nevermore.Advanced.Serialization;
using Nevermore.Mapping;
using Nevermore.Querying.AST;
using Nevermore.Util;
using Newtonsoft.Json;
using NUnit.Framework;
#pragma warning disable 618

namespace Nevermore.Tests.Util
{
    public class DataModificationQueryBuilderFixture
    {
        readonly DataModificationQueryBuilder builder;
        Func<string> idAllocator;

        public DataModificationQueryBuilderFixture()
        {
            var configuration = new RelationalStoreConfiguration("");
            configuration.RelatedDocumentsGlobalTempTableNameGenerator = () => "related_tests";
            configuration.DocumentSerializer = new NewtonsoftDocumentSerializer(configuration);
            configuration.DocumentMaps.Register(
                new TestDocumentMap(),
                new TestDocumentWithRelatedDocumentsMap(),
                new TestDocumentWithMultipleRelatedDocumentsMap(),
                new OtherMap());
            builder = new DataModificationQueryBuilder(
                configuration,
                m => idAllocator()
            );
        }

        [SetUp]
        public void SetUp()
        {
            idAllocator = () => "New-Id";
        }

        [Test]
        public void InsertSingleDocument()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result = builder.PrepareInsert(new[] {document}, InsertOptions.Default);

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithDocumentIdAlreadySet()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "SuppliedId"};

            var result = builder.PrepareInsert(
                new[] {document}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithTableNameAndHints()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result = builder.PrepareInsert(
                new[] {document},
                new InsertOptions
                {
                    TableName ="AltTableName",
                    Hint ="WITH (NOLOCK)"
                }
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertWithoutDefaultColumns()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result = builder.PrepareInsert(
                new[] {document},
                new InsertOptions { IncludeDefaultModelColumns = false}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithNoRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = null};

            var result = builder.PrepareInsert(
                new[] {document}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithOneRelatedDocument()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] {("Rel-1", typeof(Other))}};

            var result = builder.PrepareInsert(
                new[] {document}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithManyRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] {("Rel-1", typeof(Other)), ("Rel-2", typeof(Other))}};

            var result = builder.PrepareInsert(
                new[] {document}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertMultipleDocuments()
        {
            var documents = new[]
            {
                new TestDocument {AColumn = "AValue1", NotMapped = "NonMappedValue"},
                new TestDocument {AColumn = "AValue2", NotMapped = "NonMappedValue"},
            };

            int n = 0;
            idAllocator = () => "New-Id-" + (++n);
            var result = builder.PrepareInsert(
                documents
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertMultipleDocumentWithManyRelatedDocuments()
        {
            var documents = new[]
            {
                new TestDocumentWithRelatedDocuments {AColumn = "Doc1", RelatedDocumentIds = new[] {("Rel-1", typeof(Other)), ("Rel-2", typeof(Other))}},
                new TestDocumentWithRelatedDocuments {AColumn = "Doc2", RelatedDocumentIds = null},
                new TestDocumentWithRelatedDocuments {AColumn = "Doc1", RelatedDocumentIds = new[] {("Rel-2", typeof(Other)), ("Rel-3", typeof(Other))}}
            };

            int n = 0;
            idAllocator = () => "New-Id-" + (++n);
            var result = builder.PrepareInsert(
                documents
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertMultipleDocumentWithMultipleRelatedDocumentsMaps()
        {
            var documents = new[]
            {
                new TestDocumentWithMultipleRelatedDocuments
                {
                    AColumn = "Doc1",
                    RelatedDocumentIds1 = new[] {("Rel-1", typeof(Other)), ("Rel-2", typeof(Other))},
                    RelatedDocumentIds2 = new[] {("Rel-2", typeof(Other)), ("Rel-2", typeof(Other))},
                    RelatedDocumentIds3 = new[] {("Rel-3-Other", typeof(Other)), ("Rel-2", typeof(Other))}
                },
            };

            int n = 0;
            idAllocator = () => "New-Id-" + (++n);
            var result = builder.PrepareInsert(
                documents
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertDocumentWithReadOnlyColumn()
        {
            int n = 0;
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1", ReadOnly = "Value"};

            idAllocator = () => "New-Id-" + (++n);
            var result = builder.PrepareInsert(new [] { document });

            this.Assent(Format(result));
        }

        [Test]
        public void Update()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1"};

            var result = builder.PrepareUpdate(
                document
            );

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithHint()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1"};

            var result = builder.PrepareUpdate(
                document,
                new UpdateOptions { Hint = "WITH (NO LOCK)"}
            );

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithRoWVersion()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1", RowVersion = 1};

            var result = builder.PrepareUpdate(document);

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithNoRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments
            {
                AColumn = "AValue",
                Id = "Doc-1",
                RelatedDocumentIds = new (string, Type)[0]
            };

            var result = builder.PrepareUpdate(
                document
            );

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithOneRelatedDocument()
        {
            var document = new TestDocumentWithRelatedDocuments
            {
                AColumn = "AValue",
                Id = "Doc-1",
                RelatedDocumentIds = new[] {("Rel-1", typeof(Other))}
            };

            var result = builder.PrepareUpdate(
                document
            );

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithManyRelatedDocuments()
        {
            var document = new TestDocumentWithMultipleRelatedDocuments
            {
                Id = "Doc-1",
                AColumn = "Doc1",
                RelatedDocumentIds1 = new[] {("Rel-1", typeof(Other)), ("Rel-2", typeof(Other))},
                RelatedDocumentIds2 = new[] {("Rel-2", typeof(Other)), ("Rel-2", typeof(Other))},
                RelatedDocumentIds3 = new[] {("Rel-3-Other", typeof(Other)), ("Rel-2", typeof(Other))}
            };

            var result = builder.PrepareUpdate(
                document
            );

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByDocument()
        {
            var document = new TestDocument {Id = "Doc-1",};
            var result = builder.PrepareDelete(document);

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteById()
        {
            var result = builder.PrepareDelete<TestDocument>("Doc-1");
            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByWhere()
        {
            var result = builder.PrepareDelete(typeof(TestDocument), new Where(new UnaryWhereClause(new WhereFieldReference("Foo"), UnarySqlOperand.GreaterThan, "1")), new CommandParameterValues());
            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByDocumentWithOneRelatedTable()
        {
            var document = new TestDocumentWithRelatedDocuments {Id = "Doc-1",};
            var result = builder.PrepareDelete(document);

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByDocumentWithManyRelatedTables()
        {
            var result = builder.PrepareDelete<TestDocumentWithMultipleRelatedDocuments>("Doc-1");

            this.Assent(Format(result));
        }

        string Format(PreparedCommand[] results)
        {
            var sb = new StringBuilder();
            foreach (var command in results)
            {
                sb.Append(Format(command));
            }
            return sb.ToString();
        }

        string Format(PreparedCommand result)
        {
            var receivedParameterValues = result.ParameterValues.Select(v => $"@{v.Key}={FormatValue(v.Value)}");
            return result.Statement + "\r\n" + string.Join("\r\n", receivedParameterValues);
        }

        object FormatValue(object paramValue)
        {
            if (paramValue is TextReader r)
            {
                return r.ReadToEnd();
            }

            return paramValue;
        }

        class TestDocument
        {
            public string Id { get; set; }
            public string AColumn { get; set; }
            public string NotMapped { get; set; }
            public string ReadOnly { get; set; }
            public int RowVersion { get; set; }
        }

        class TestDocumentWithRelatedDocuments
        {
            public string Id { get; set; }
            public string AColumn { get; set; }

            [JsonIgnore]
            public IEnumerable<(string, Type)> RelatedDocumentIds { get; set; }
        }

        class TestDocumentWithMultipleRelatedDocuments
        {
            public string Id { get; set; }
            public string AColumn { get; set; }

            [JsonIgnore]
            public IEnumerable<(string, Type)> RelatedDocumentIds1 { get; set; }

            [JsonIgnore]
            public IEnumerable<(string, Type)> RelatedDocumentIds2 { get; set; }

            [JsonIgnore]
            public IEnumerable<(string, Type)> RelatedDocumentIds3 { get; set; }
        }


        class Other
        {
            public string Id { get; set; }
        }

        class TestDocumentMap : DocumentMap<TestDocument>
        {
            public TestDocumentMap()
            {
                TableName = "TestDocumentTbl";
                Column(t => t.AColumn);
                Column(t => t.ReadOnly).LoadOnly();
                RowVersion(t => t.RowVersion);
            }
        }

        class TestDocumentWithRelatedDocumentsMap : DocumentMap<TestDocumentWithRelatedDocuments>
        {
            public TestDocumentWithRelatedDocumentsMap()
            {
                TableName = "TestDocumentTbl";
                Column(t => t.AColumn);
                RelatedDocuments(t => t.RelatedDocumentIds);
            }
        }

        class TestDocumentWithMultipleRelatedDocumentsMap : DocumentMap<TestDocumentWithMultipleRelatedDocuments>
        {
            public TestDocumentWithMultipleRelatedDocumentsMap()
            {
                TableName = "TestDocumentTbl";
                Column(t => t.AColumn);
                RelatedDocuments(t => t.RelatedDocumentIds1);
                RelatedDocuments(t => t.RelatedDocumentIds2);
                RelatedDocuments(t => t.RelatedDocumentIds3, "OtherRelatedTable");
            }
        }

        class OtherMap : DocumentMap<Other>
        {
            public OtherMap()
            {
                TableName = "OtherTbl";
            }
        }
    }
}