using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseMethodOperationTests : LinqTestBase
    {
        [Fact]
        public void StringContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.Contains("Bar"));

            AssertLikeResult(result, captures, "%Bar%");
        }

        [Fact]
        public void StartsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.StartsWith("Bar"));

            AssertLikeResult(result, captures, "Bar%");
        }
        
        [Fact]
        public void EndsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.EndsWith("Bar"));

            AssertLikeResult(result, captures, "%Bar");
        }
        
        [Fact]
        public void ArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

     

        [Fact]
        public void IListArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            IList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }
        
        [Fact]
        public void IReadOnlyListArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            IReadOnlyList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }
        
        [Fact]
        public void ListContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var values = new List<int> {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

        
        static void AssertLikeResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures, string expected)
        {
            var (parameters, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([String] LIKE @string)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("string");
            paramValues.Should().Contain("string", expected);
        }
        
        static void AssertContainsResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures)
        {
            var (_, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] IN (@int0, @int1, @int2))
ORDER BY [Id]");

            paramValues.Should().Contain("int0", "1");
            paramValues.Should().Contain("int1", "2");
            paramValues.Should().Contain("int2", "3");
        }

 
    }
}