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


        class TestDocumentMap : DocumentMap<TestDocument>
        {
            public TestDocumentMap()
            {
                Column(t => t.AColumn);
            }
        }
    }
}