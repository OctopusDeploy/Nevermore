using System.Linq;
using Nevermore.Contracts;
using Nevermore.Joins;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.Joins
{
    public class JoinExtensionsFixture
    {
        [Fact]
        public void ShouldAddInnerJoinToQueryBuilder()
        {
            var transaction = Substitute.For<IRelationalTransaction>();
            var leftQuery = new QueryBuilder<IDocument>(transaction, "Orders");
            var rightQuery = new QueryBuilder<IDocument>(transaction, "Accounts");

            leftQuery.InnerJoin(rightQuery);

            Assert.NotEmpty(leftQuery.QueryGenerator.Joins);

            var join = leftQuery.QueryGenerator.Joins.Single();
            Assert.Equal(rightQuery.QueryGenerator, join.RightQuery);
            Assert.Equal(JoinType.InnerJoin, join.JoinType);
        }

        [Fact]
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
                Assert.NotEmpty(lastJoin.JoinClauses);

                var joinClause = lastJoin.JoinClauses.Single();
                var expected = $"{i} = {i}";
                Assert.Equal(expected, joinClause.ToString());
            }
        }
    }
}