using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseLogicalClausesTests : LinqTestBase
    {
        [Fact]
        public void And()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int < 2 && f.String == "bar");

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] < @int)
AND ([String] = @string)
ORDER BY [Id]");
        }
        
        [Fact(Skip = "Queries where the same property is specified twice in the where clause are not yet supported")]
        public void AndWithTheSameProperty()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int > 2 && f.Int < 4);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] < @int)
AND ([Int] = @int2)
ORDER BY [Id]");
        }
        
    }
}