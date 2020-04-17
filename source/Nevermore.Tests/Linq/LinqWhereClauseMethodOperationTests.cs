using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseMethodOperationTests : LinqTestBase
    {
        [Test]
        public void StringContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.Contains("Bar"));

            AssertLikeResult(result, captures, "%Bar%");
        }

        [Test]
        public void StartsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.StartsWith("Bar"));

            AssertLikeResult(result, captures, "Bar%");
        }

        [Test]
        public void EndsWith()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(b => b.String.EndsWith("Bar"));

            AssertLikeResult(result, captures, "%Bar");
        }

        [Test]
        public void ArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

     

        [Test]
        public void IListArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            IList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

        [Test]
        public void IReadOnlyListArrayContains()
        {
            var (builder, captures) = NewQueryBuilder();

            IReadOnlyList<int> values = new[] {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

        [Test]
        public void ListContains()
        {
            var (builder, captures) = NewQueryBuilder();

            var values = new List<int> {1, 2, 3};
            var result = builder.Where(b => values.Contains(b.Int));

            AssertContainsResult(result, captures);
        }

        [Test]
        public void StringListContains()
        {
            var (builder, (_, paramValues)) = NewQueryBuilder();

            var values = new List<string> {"a", "B", "3"};
            var result = builder.Where(b => values.Contains(b.String));

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([String] IN (@string0, @string1, @string2))
ORDER BY [Id]");

            paramValues.Should().Contain("string0", "a");
            paramValues.Should().Contain("string1", "B");
            paramValues.Should().Contain("string2", "3");
        }

        static void AssertLikeResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures, string expected)
        {
            var (parameters, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([String] LIKE @string)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("string");
            paramValues.Should().Contain("string", expected);
        }

        static void AssertContainsResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures)
        {
            var (_, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT *
FROM dbo.[Foo]
WHERE ([Int] IN (@int0, @int1, @int2))
ORDER BY [Id]");

            paramValues.Should().Contain("int0", "1");
            paramValues.Should().Contain("int1", "2");
            paramValues.Should().Contain("int2", "3");
        }

 
    }
}