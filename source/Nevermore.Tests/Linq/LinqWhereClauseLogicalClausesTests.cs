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
        
        [Fact]
        public void AndWithTheSameProperty()
        {
            var (builder, _) = NewQueryBuilder(new ParameterNameGenerator());

            var result = builder.Where(f => f.Int > 2 && f.Int < 4);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] > @int_0)
AND ([Int] < @int_1)
ORDER BY [Id]");
        }
        
    }
}