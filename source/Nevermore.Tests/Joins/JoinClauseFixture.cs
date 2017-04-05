using Nevermore.Joins;
using Xunit;

namespace Nevermore.Tests.Joins
{
    public class JoinClauseFixture
    {
        [Fact]
        public void ShouldReturnEqualsString()
        {
            var target = new JoinClause("FieldA", JoinOperand.Equal, "FieldB");

            const string expected = "FieldA = FieldB";
            var actual = target.ToString();

            Assert.Equal(expected, actual);
        }
    }
}