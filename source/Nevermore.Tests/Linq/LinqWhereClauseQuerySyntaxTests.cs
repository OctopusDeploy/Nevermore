using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseQuerySyntaxTests : LinqTestBase
    {
        [Fact]
        public void SingleWhereClause()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where f.Int < 2
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] < @int) ORDER BY [Id]");
        }

        [Fact(Skip = "Queries where the same property is specified twice in the where clause are not yet supported")]
        public void MultipleWhereClausesWithTheSameProperty()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where f.Int < 2
                where f.Int > 4
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] < @int) ORDER BY [Id]");
        }

        [Fact]
        public void MultipleWhereClausesWithDifferentProperties()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where f.Int < 2
                where f.String == "bar"
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] < @int) AND ([String] = @string) ORDER BY [Id]");
        }

        [Fact]
        public void StringWhereClause()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where "N = 100"
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE (N = 100) ORDER BY [Id]");
        }
    }
}