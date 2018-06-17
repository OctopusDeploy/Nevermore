using FluentAssertions;
using Nevermore.Tests.Query;
using NUnit.Framework;

namespace Nevermore.Tests.Linq
{
    public class LinqOrderByClauseTests : LinqTestBase
    {
        [Test]
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
        
        [Test]
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
        
        [Test]
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
        
        [Test]
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
        
        [Test]
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
        
        [Test]
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
        
        [Test]
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
        
        [Test]
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