using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Assent;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Util;
using Newtonsoft.Json;
using Xunit;

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
                new TestDocumentWithMultipleRelatedDocumentsMap()
            });
            builder = new DataModificationQueryBuilder(
                mappings,
                new JsonSerializerSettings()
            );
        }

        [Fact]
        public void InsertSingleDocument()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result =  builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void InsertSingleDocumentWithDocumentIdAlreadySet()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id = "SuppliedId"};

            var result =  builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void InsertSingleDocumentWithTableNameAndHints()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue"};

            var result =  builder.CreateInsert(
                new[] {document},
                "AltTableName",
                "WITH (NOLOCK)",
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
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

        [Fact]
        public void InsertSingleDocumentWithOneRelatedDocument()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] { "Rel-1"}};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void InsertSingleDocumentWithManyRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments {AColumn = "AValue", RelatedDocumentIds = new[] {"Rel-1", "Rel-2"}};

            var result = builder.CreateInsert(
                new[] {document},
                null,
                null,
                map => "New-Id",
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void InsertMultipleDocuments()
        {
            var documents = new[]
            {
                new TestDocument {AColumn = "AValue1", NotMapped = "NonMappedValue"},
                new TestDocument {AColumn = "AValue2", NotMapped = "NonMappedValue"},
            };

            int n = 0;
            var result =  builder.CreateInsert(
                documents,
                null,
                null,
                map => "New-Id-" + (++n),
                true
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void InsertMultipleDocumentWithManyRelatedDocuments()
        {
            var documents = new[]
            {
                new TestDocumentWithRelatedDocuments {AColumn = "Doc1", RelatedDocumentIds = new[] {"Rel-1", "Rel-2"}},
                new TestDocumentWithRelatedDocuments {AColumn = "Doc2", RelatedDocumentIds = null},
                new TestDocumentWithRelatedDocuments {AColumn = "Doc1", RelatedDocumentIds = new[] {"Rel-2", "Rel-3"}}
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
        
        [Fact]
        public void InsertMultipleDocumentWithMultipleRelatedDocumentsMaps()
        {
            var documents = new[]
            {
                new TestDocumentWithMultipleRelatedDocuments
                {
                    AColumn = "Doc1", 
                    RelatedDocumentIds1 = new[] {"Rel-1", "Rel-2"},
                    RelatedDocumentIds2 = new[] {"Rel-2", "Rel-2"},
                    RelatedDocumentIds3 = new[] {"Rel-3-Other", "Rel-2"}
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
		
        [Fact]
        public void Update()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id="Doc-1"};

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void UpdateWithHint()
        {
            var document = new TestDocument {AColumn = "AValue", NotMapped = "NonMappedValue", Id="Doc-1"};

            var result =  builder.CreateUpdate(
                document,
                "WITH (NO LOCK)"
            );
            
            this.Assent(Format(result));
        }
        
        [Fact]
        public void UpdateWithNoRelatedDocuments()
        {
            var document = new TestDocumentWithRelatedDocuments
            {
                AColumn = "AValue", 
                Id="Doc-1",
                RelatedDocumentIds = new string[0]
            };

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }
        
        [Fact]
        public void UpdateWithOneRelatedDocument()
        {
            var document = new TestDocumentWithRelatedDocuments
            {
                AColumn = "AValue", 
                Id="Doc-1",
                RelatedDocumentIds = new[] { "Rel-1"}
            };

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }
        
        [Fact]
        public void UpdateWithManyRelatedDocuments()
        {
            var document = new TestDocumentWithMultipleRelatedDocuments
            {
                Id = "Doc-1",
                AColumn = "Doc1",
                RelatedDocumentIds1 = new[] {"Rel-1", "Rel-2"},
                RelatedDocumentIds2 = new[] {"Rel-2", "Rel-2"},
                RelatedDocumentIds3 = new[] {"Rel-3-Other", "Rel-2"}
            };

            var result = builder.CreateUpdate(
                document,
                null
            );

            this.Assent(Format(result));
        }

        [Fact]
        public void DeleteByDocument()
        {
            var document = new TestDocument {Id="Doc-1",};
            var result = builder.CreateDelete(document);
            
            this.Assent(Format(result));
        }
        
        [Fact]
        public void DeleteById()
        {
            var result = builder.CreateDelete<TestDocument>("Doc-1");
            this.Assent(Format(result));
        }
        
        [Fact]
        public void DeleteByWhere()
        {
            var result = builder.CreateDelete(typeof(TestDocument), new Where(new UnaryWhereClause(new WhereFieldReference("Foo"), UnarySqlOperand.GreaterThan, "1")));
            this.Assent(result);
        }
        
        [Fact]
        public void DeleteByDocumentWithOneRelatedTable()
        {
            var document = new TestDocumentWithRelatedDocuments {Id="Doc-1",};
            var result = builder.CreateDelete(document);
            
            this.Assent(Format(result));
        }
        
        [Fact]
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
            public IEnumerable<string> RelatedDocumentIds { get; set; }
        }

        class TestDocumentWithMultipleRelatedDocuments : IId
        {
            public string Id { get; set; }
            public string AColumn { get; set; }
            
            [JsonIgnore]
            public IEnumerable<string> RelatedDocumentIds1 { get; set; }
            [JsonIgnore]
            public IEnumerable<string> RelatedDocumentIds2 { get; set; }
            [JsonIgnore]
            public IEnumerable<string> RelatedDocumentIds3 { get; set; }
        }


        class TestDocumentMap : DocumentMap<TestDocument>
        {
            public TestDocumentMap()
            {
                Column(t => t.AColumn);
            }
        }

        class TestDocumentWithRelatedDocumentsMap : DocumentMap<TestDocumentWithRelatedDocuments>
        {
            public TestDocumentWithRelatedDocumentsMap()
            {
                Column(t => t.AColumn);
                RelatedDocuments(t => t.RelatedDocumentIds);
            }
        }

        class TestDocumentWithMultipleRelatedDocumentsMap : DocumentMap<TestDocumentWithMultipleRelatedDocuments>
        {
            public TestDocumentWithMultipleRelatedDocumentsMap()
            {
                Column(t => t.AColumn);
                RelatedDocuments(t => t.RelatedDocumentIds1);
                RelatedDocuments(t => t.RelatedDocumentIds2);
                RelatedDocuments(t => t.RelatedDocumentIds3, "OtherRelatedTable");
            }
        }
    }
}