using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqOrderByClauseTests : LinqTestBase
    {
        [Fact]
        public void SingleOrderBy()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.OrderBy(f => f.Int);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int]");
        }
        
        [Fact]
        public void SingleOrderByDesc()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.OrderByDescending(f => f.Int);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int] DESC");
        }
        
        [Fact]
        public void SingleQuerySyntaxOrderBy()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from b in builder
                orderby b.Int
                select b;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int]");
        }
        
        [Fact]
        public void SingleQuerySyntaxOrderByDesc()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from b in builder
                orderby b.Int descending 
                select b;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int] DESC");
        }
        
        [Fact]
        public void MultipleOrderBy()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.OrderBy(f => f.Int).ThenBy(f => f.String);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int], [String]");
        }
        
        [Fact]
        public void MultipleOrderByDesc()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.OrderBy(f => f.Int).ThenByDescending(f => f.String);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int], [String] DESC");
        }
        
        [Fact]
        public void MultipleQuerySyntaxOrderBy()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from b in builder
                orderby b.Int, b.String
                select b;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int], [String]");
        }
        
        [Fact]
        public void MultipleQuerySyntaxOrderByDesc()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from b in builder
                orderby b.Int, b.String descending 
                select b;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
ORDER BY [Int], [String] DESC");
        }
    }
}