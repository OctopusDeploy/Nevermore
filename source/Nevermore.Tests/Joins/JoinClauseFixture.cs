using Nevermore.Joins;
using NUnit.Framework;

namespace Nevermore.Tests.Joins
{
    [TestFixture]
    public class JoinClauseFixture
    {
        [Test]
        public void ShouldReturnEqualsString()
        {
            var target = new JoinClause("FieldA", JoinOperand.Equal, "FieldB");

            const string expected = "FieldA = FieldB";
            var actual = target.ToString();

            Assert.AreEqual(expected, actual);
        }
    }
}