using FluentAssertions;
using Nevermore.AST;
using NUnit.Framework;

namespace Nevermore.Tests.Joins
{
    public class JoinClauseFixture
    {
        [Test]
        public void ShouldReturnEqualsString()
        {
            var target = new JoinClause("FieldA", JoinOperand.Equal, "FieldB");

            const string expected = "t1.[FieldA] = t2.[FieldB]";
            var actual = target.GenerateSql("t1", "t2");

            actual.Should().Be(expected);
        }
    }
}