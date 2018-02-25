using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseComparisonTypeTests : LinqTestBase
    {
        [Fact]
        public void Less()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int < 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] < @int) ORDER BY [Id]");
        }
        
        [Fact]
        public void LessOrEqual()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int <= 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] <= @int) ORDER BY [Id]");
        }
        
        
        [Fact]
        public void More()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int > 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] > @int) ORDER BY [Id]");
        }
        
        [Fact]
        public void MoreThan()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int >= 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] >= @int) ORDER BY [Id]");
        }
        
        [Fact]
        public void NotEqual()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int != 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] <> @int) ORDER BY [Id]");
        }
        
    }
}