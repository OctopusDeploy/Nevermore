using System.Linq;
using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseStringOperationTests : LinqTestBase
    {
        [Fact]
        public void StringContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.Contains("Bar"));

            AssertResult(result, captures, "%Bar%");
        }

        [Fact]
        public void StartsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.StartsWith("Bar"));

            AssertResult(result, captures, "Bar%");
        }
        
        [Fact]
        public void EndsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.EndsWith("Bar"));

            AssertResult(result, captures, "%Bar");
        }
        
        static void AssertResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures, string expected)
        {
            var (parameters, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([String] LIKE @string) ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("string");
            paramValues.Should().Contain("string", expected);
        }
 
    }
}