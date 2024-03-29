﻿using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseQuerySyntaxTests : LinqTestBase
    {
        [Test]
        public void SingleWhereClause()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where f.Int < 2
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Int] < @int)
ORDER BY [Id]");
        }

        [Test]
        public void MultipleWhereClausesWithTheSameProperty()
        {
            var (builder, _) = NewQueryBuilder(new UniqueParameterNameGenerator());

            var result = from f in builder
                where f.Int < 2
                where f.Int > 4
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Int] < @int)
AND ([Int] > @int_1)
ORDER BY [Id]");
        }

        [Test]
        public void MultipleWhereClausesWithDifferentProperties()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where f.Int < 2
                where f.String == "bar"
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Int] < @int)
AND ([String] = @string)
ORDER BY [Id]");
        }

        [Test]
        public void StringWhereClause()
        {
            var (builder, _) = NewQueryBuilder();

            var result = from f in builder
                where "N = 100"
                select f;

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE (N = 100)
ORDER BY [Id]");
        }
    }
}