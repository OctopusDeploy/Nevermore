using Nevermore.AST;
using Xunit;

namespace Nevermore.Tests.Joins
{
    public class JoinClauseFixture
    {
        [Fact]
        public void ShouldReturnEqualsString()
        {
            var target = new JoinClause("FieldA", JoinOperand.Equal, "FieldB");

            const string expected = "t1.[FieldA] = t2.[FieldB]";
            var actual = target.GenerateSql("t1", "t2");

            Assert.Equal(expected, actual);
        }
    }
}