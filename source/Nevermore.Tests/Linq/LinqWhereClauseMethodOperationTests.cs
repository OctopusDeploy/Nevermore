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
            var builder = NewQueryBuilder();

            var result = builder.Where(b => b.String.Contains("Bar"));

            AssertLikeResult(result, "%Bar%");
        }

        [Fact]
        public void StartsWith()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(b => b.String.StartsWith("Bar"));

            AssertLikeResult(result, "Bar%");
        }
        
        [Fact]
        public void EndsWith()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(b => b.String.EndsWith("Bar"));

            AssertLikeResult(result, "%Bar");
        }
        
        [Fact]
        public void ArrayContains()
        {
            var builder = NewQueryBuilder();

            var values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result);
        }

     

        [Fact]
        public void IListArrayContains()
        {
            var builder = NewQueryBuilder();

            IList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result);
        }
        
        [Fact]
        public void IReadOnlyListArrayContains()
        {
            var builder = NewQueryBuilder();

            IReadOnlyList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result);
        }
        
        [Fact]
        public void ListContains()
        {
            var builder = NewQueryBuilder();

            var values = new List<int> {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result);
        }

        
        static void AssertLikeResult(IQueryBuilder<Foo> result, string expected)
        {
            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([String] LIKE @string) ORDER BY [Id]");

            result.QueryGenerator.QueryParameters.Single().Key.Should().Be("string");
            result.QueryGenerator.QueryParameters.Should().Contain("string", expected);
        }
        
        static void AssertContainsResult(IQueryBuilder<Foo> result)
        {
            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] IN (@int0, @int1, @int2)) ORDER BY [Id]");

            result.QueryGenerator.QueryParameters.Should().Contain("int0", "1");
            result.QueryGenerator.QueryParameters.Should().Contain("int1", "2");
            result.QueryGenerator.QueryParameters.Should().Contain("int2", "3");
        }

 
    }
}