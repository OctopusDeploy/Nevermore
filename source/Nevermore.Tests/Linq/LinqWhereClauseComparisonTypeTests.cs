using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseComparisonTypeTests : LinqTestBase
    {
        [Test]
        public void Less()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int < 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] < @int)
ORDER BY [Id]");
        }
        
        [Test]
        public void LessOrEqual()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int <= 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] <= @int)
ORDER BY [Id]");
        }
        
        
        [Test]
        public void More()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int > 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] > @int)
ORDER BY [Id]");
        }
        
        [Test]
        public void MoreThan()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int >= 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] >= @int)
ORDER BY [Id]");
        }
        
        [Test]
        public void NotEqual()
        {
            var (builder, _) = NewQueryBuilder();

            var result = builder.Where(f => f.Int != 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] <> @int)
ORDER BY [Id]");
        }

        [Test]
        public void IsNull()
        {
            NewQueryBuilder().builder.Where(f => f.String == null).DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([String] is null)
ORDER BY [Id]");
            
            NewQueryBuilder().builder.Where(f => f.String != null).DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([String] is not null)
ORDER BY [Id]");
        }
    }
}