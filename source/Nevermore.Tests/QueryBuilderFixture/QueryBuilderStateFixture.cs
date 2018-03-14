using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class QueryBuilderStateFixture
    {
        readonly IRelationalTransaction transaction;
        readonly List<string> executedQueries = new List<string>();

        public QueryBuilderStateFixture()
        {
            transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Do<string>(q => executedQueries.Add(q)), Arg.Any<CommandParameterValues>());
            transaction.ExecuteReader<IDocument>(Arg.Do<string>(q => executedQueries.Add(q)), Arg.Any<CommandParameterValues>());
        }

        [Fact]
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

        [Fact]
        public void ShouldNotModifyQueryBuilderStateFromAny()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Any();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT COUNT(*)
FROM dbo.[Accounts]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Fact]
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

        [Fact]
        public void ShouldNotModifyQueryBuilderStateFromFirst()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.First();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Fact]
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
WHERE ([RowNum] >= @_minrow)
AND ([RowNum] <= @_maxrow)
ORDER BY [RowNum]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Fact]
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
WHERE ([RowNum] >= @_minrow)
AND ([RowNum] <= @_maxrow)
ORDER BY [RowNum]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
        public void ShouldNotModifyQueryBuilderStateFromToDictionary()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToDictionary(d => d.Id);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM dbo.[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM dbo.[Accounts]
ORDER BY [Id]");
        }

        string LastExecutedQuery() => executedQueries.Last();

        IQueryBuilder<IDocument> QueryBuilder(string tableName)
        {
            return new QueryBuilder<IDocument, TableSelectBuilder>(new TableSelectBuilder(new SimpleTableSource(tableName)), transaction, new TableAliasGenerator(), new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }
    }
}