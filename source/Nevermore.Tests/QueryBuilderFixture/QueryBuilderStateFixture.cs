using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class QueryBuilderStateFixture
    {
        readonly IReadTransaction transaction;
        readonly List<string> executedQueries = new List<string>();

        public QueryBuilderStateFixture()
        {
            transaction = Substitute.For<IReadTransaction>();
            transaction.ExecuteScalar<int>(Arg.Do<string>(q => executedQueries.Add(q)), Arg.Any<CommandParameterValues>());
            transaction.Stream<object>(Arg.Do<string>(q => executedQueries.Add(q)), Arg.Any<CommandParameterValues>());
        }

        [SetUp]
        public void SetUp()
        {
            executedQueries.Clear();
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromCount()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Count();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT COUNT(*)
FROM dbo.[Accounts]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromAny()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Any();

            LastExecutedQuery().ShouldBeEquivalentTo(@"IF EXISTS(SELECT *
FROM dbo.[Accounts])
    SELECT @true_0
ELSE
    SELECT @false_1");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromTake()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromFirst()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.FirstOrDefault();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToListPageinated()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToList(10, 20);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM (
    SELECT *,
    ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
    FROM dbo.[Accounts]
) ALIAS_GENERATED_1
WHERE ([RowNum] >= @_minrow_0)
AND ([RowNum] <= @_maxrow_1)
ORDER BY [RowNum]");

            queryBuilder.ParameterValues.Count.ShouldBeEquivalentTo(0);
            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToListPageinatedWithCount()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToList(10, 20, out var _);

            executedQueries.Count.ShouldBeEquivalentTo(2);
            executedQueries.First().ShouldBeEquivalentTo(@"SELECT COUNT(*)
FROM dbo.[Accounts]");

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM (
    SELECT *,
    ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
    FROM dbo.[Accounts]
) ALIAS_GENERATED_1
WHERE ([RowNum] >= @_minrow_0)
AND ([RowNum] <= @_maxrow_1)
ORDER BY [RowNum]");

            queryBuilder.ParameterValues.Count.ShouldBeEquivalentTo(0);

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToList()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromStream()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Stream();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToDictionary()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToDictionary(d => d.GetHashCode().ToString());

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldAddWhereClauseToExistingQueryBuilder()
        {
            transaction.TableQuery<object>().Returns(TableQueryBuilder("Accounts"));

            var query = transaction.Query<object>();

            // Don't capture the new query builder here - there is code in Octopus that is structured this way
            query.Where("Id", UnarySqlOperand.Equal, 1);
            query.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
WHERE ([Id] = @id_0)
ORDER BY [Id]");
        }

        string LastExecutedQuery() => executedQueries.Last();

        IQueryBuilder<object> QueryBuilder(string tableName)
        {
            return TableQueryBuilder(tableName)
                // This convert the table query builder to a normal query builder. 
                // If you don't do this, then you are only testing the implementation of ITableSourceQueryBuilder, which creates a brand new query builder from most modifications.
                .AsType<object>();
        }

        ITableSourceQueryBuilder<object> TableQueryBuilder(string tableName)
        {
            return new TableSourceQueryBuilder<object>(tableName, transaction, new TableAliasGenerator(), new UniqueParameterNameGenerator(), new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }
    }
}