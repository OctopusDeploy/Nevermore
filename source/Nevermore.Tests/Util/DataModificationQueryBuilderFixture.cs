using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Assent;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Util;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.Tests.Util
{
    public class DataModificationQueryBuilderFixture
    {
        readonly DataModificationQueryBuilder builder;

        public DataModificationQueryBuilderFixture()
        {
            var mappings = new RelationalMappings();
            mappings.Install(new DocumentMap[]
            {
                new TestDocumentMap(),
                new TestDocumentWithRelatedDocumentsMap(),
                new TestDocumentWithMultipleRelatedDocumentsMap(),
                new OtherMap()
            });
            builder = new DataModificationQueryBuilder(
                mappings,
                new JsonSerializerSettings()
            );
        }

        [Test]
        public void InsertSingleDocument()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithDocumentIdAlreadySet()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "SuppliedId"};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithTableNameAndHints()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result = builder.CreateInsert(
                new[] {document},
                "AltTableName",
                "WITH (NOLOCK)",
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithNoRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = null};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithOneRelatedDocument()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] {("Rel-1", typeof(Other))}};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void InsertSingleDocumentWithManyRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] {("Rel-1", typeof(Other)), ("Rel-2", typeof(Other))}};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
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
            var result = builder.CreateInsert(
                documents,
                null,
                null,
                map => "New-Id-" + (++n),
                true
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
            var result = builder.CreateInsert(
                documents,
                null,
                null,
                map => "New-Id-" + (++n),
                true
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
            var result = builder.CreateInsert(
                documents,
                null,
                null,
                map => "New-Id-" + (++n),
                true
            );

            this.Assent(Format(result));
        }

        [Test]
        public void Update()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1"};

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }

        [Test]
        public void UpdateWithHint()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "Doc-1"};

            var result = builder.CreateUpdate(
                document,
                "WITH (NO LOCK)"
            );

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

            var result = builder.CreateUpdate(
                document,
                null
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

            var result = builder.CreateUpdate(
                document,
                null
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

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByDocument()
        {
            var document = new TestDocument {Id = "Doc-1",};
            var result = builder.CreateDelete(document);

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteById()
        {
            var result = builder.CreateDelete<TestDocument>("Doc-1");
            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByWhere()
        {
            var result = builder.CreateDelete(typeof(TestDocument), new Where(new UnaryWhereClause(new WhereFieldReference("Foo"), UnarySqlOperand.GreaterThan, "1")));
            this.Assent(result);
        }

        [Test]
        public void DeleteByDocumentWithOneRelatedTable()
        {
            var document = new TestDocumentWithRelatedDocuments {Id = "Doc-1",};
            var result = builder.CreateDelete(document);

            this.Assent(Format(result));
        }

        [Test]
        public void DeleteByDocumentWithManyRelatedTables()
        {
            var result = builder.CreateDelete<TestDocumentWithRelatedDocuments>("Doc-1");

            this.Assent(Format(result));
        }


        string Format((DocumentMap, string statement, CommandParameterValues parameterValues) result)
            => Format((result.statement, result.parameterValues));

        string Format((string statement, CommandParameterValues parameterValues) result)
        {
            var recievedParameterValues = result.parameterValues.Select(v => $"@{v.Key}={v.Value}");
            return result.statement + "\r\n" + string.Join("\r\n", recievedParameterValues);
        }


        class TestDocument : IId
        {
            public string Id { get; set; }
            public string AColumn { get; set; }
            public string NotMapped { get; set; }
        }

        class TestDocumentWithRelatedDocuments : IId
        {
            public string Id { get; set; }
            public string AColumn { get; set; }

            [JsonIgnore]
            public IEnumerable<(string, Type)> RelatedDocumentIds { get; set; }
        }

        class TestDocumentWithMultipleRelatedDocuments : IId
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
        }

        class TestDocumentMap : DocumentMap<TestDocument>
        {
            public TestDocumentMap()
            {
                TableName = "TestDocument";
                Column(t => t.AColumn);
            }
        }

        class TestDocumentWithRelatedDocumentsMap : DocumentMap<TestDocumentWithRelatedDocuments>
        {
            public TestDocumentWithRelatedDocumentsMap()
            {
                TableName = "TestDocument";
                Column(t => t.AColumn);
                RelatedDocuments(t => t.RelatedDocumentIds);
            }
        }

        class TestDocumentWithMultipleRelatedDocumentsMap : DocumentMap<TestDocumentWithMultipleRelatedDocuments>
        {
            public TestDocumentWithMultipleRelatedDocumentsMap()
            {
                TableName = "TestDocument";
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