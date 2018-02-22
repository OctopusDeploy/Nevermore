using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqClausesAreImmutableTests : LinqTestBase
    {
        [Fact]
        public void Where()
        {
            var (builder, _) = NewQueryBuilder();

            var _ = builder.Where(f => f.Int < 2);

            builder.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] ORDER BY [Id]");
        }
        
        [Fact]
        public void OrderBy()
        {
            var (builder, _) = NewQueryBuilder();

            var _ = builder.OrderBy(f => f.Int);

            builder.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] ORDER BY [Id]");
        }
        
        [Fact]
        public void OrderByDesc()
        {
            var (builder, _) = NewQueryBuilder();

            var _ = builder.OrderByDescending(f => f.Int);

            builder.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] ORDER BY [Id]");
        }
        
        
        [Fact]
        public void ThenBy()
        {
            var (builder, _) = NewQueryBuilder();

            var ordered = builder.OrderBy(f => f.Int);

            var _ = ordered.ThenBy(f => f.String);

            builder.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] ORDER BY [Int]");
        }
        
        [Fact]
        public void ThenByDesc()
        {
            var (builder, _) = NewQueryBuilder();
            var ordered = builder.OrderBy(f => f.String);

            var _ = ordered.ThenByDescending(f => f.Int);

            builder.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] ORDER BY [Int]");
        }

        
    }
}