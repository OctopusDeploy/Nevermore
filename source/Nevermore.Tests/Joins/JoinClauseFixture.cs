using FluentAssertions;
using Nevermore.Querying.AST;
using NUnit.Framework;

namespace Nevermore.Tests.Joins
{
    public class JoinClauseFixture
    {
        [Test]
        public void ShouldReturnEqualsString()
        {
            var target = new JoinClause("t1", "FieldA", JoinOperand.Equal, "t2","FieldB");

            const string expected = "t1.[FieldA] = t2.[FieldB]";
            var actual = target.GenerateSql();

            actual.Should().Be(expected);
        }
    }
}