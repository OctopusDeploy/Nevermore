using System.Linq;
using Nevermore.Joins;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests.Joins
{
    [TestFixture]
    public class JoinExtensionsFixture
    {
        [Test]
        public void ShouldAddInnerJoinToQueryBuilder()
        {
            var transaction = Substitute.For<IRelationalTransaction>();
            var leftQuery = new QueryBuilder<IDocument>(transaction, "Orders");
            var rightQuery = new QueryBuilder<IDocument>(transaction, "Accounts");

            leftQuery.InnerJoin(rightQuery);

            Assert.IsNotEmpty(leftQuery.QueryGenerator.Joins);

            var join = leftQuery.QueryGenerator.Joins.Single();
            Assert.AreEqual(rightQuery.QueryGenerator, join.RightQuery);
            Assert.AreEqual(JoinType.InnerJoin, join.JoinType);
        }

        [Test]
        public void ShouldAddClauseToLastJoin()
        {
            var transaction = Substitute.For<IRelationalTransaction>();
            var leftQuery = new QueryBuilder<IDocument>(transaction, "Orders");
            var rightQuery = new QueryBuilder<IDocument>(transaction, "Accounts");

            for (var i = 0; i < 1000; i++)
            {
                leftQuery.InnerJoin(rightQuery)
                    .On(i.ToString(), JoinOperand.Equal, i.ToString());

                var lastJoin = leftQuery.QueryGenerator.Joins.Last();
                Assert.IsNotEmpty(lastJoin.JoinClauses);

                var joinClause = lastJoin.JoinClauses.Single();
                var expected = $"{i} = {i}";
                Assert.AreEqual(expected, joinClause.ToString());
            }
        }
    }
}