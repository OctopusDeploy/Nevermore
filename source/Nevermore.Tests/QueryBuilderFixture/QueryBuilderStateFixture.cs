﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using NSubstitute;
using NUnit.Framework;

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
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
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
FROM [dbo].[Accounts]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromAny()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Any();

            LastExecutedQuery().ShouldBeEquivalentTo(@"IF EXISTS(SELECT *
FROM [dbo].[Accounts])
    SELECT @true
ELSE
    SELECT @false");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }
        
        
        [Test]
        public void ShouldNotModifyQueryBuilderStateFromDistinct()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Distinct();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT DISTINCT *
FROM [dbo].[Accounts]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromTake()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM [dbo].[Accounts]
ORDER BY [Id]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromFirst()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.FirstOrDefault();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM [dbo].[Accounts]
ORDER BY [Id]");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
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
    FROM [dbo].[Accounts]
) ALIAS_GENERATED_1
WHERE ([RowNum] >= @_minrow)
AND ([RowNum] <= @_maxrow)
ORDER BY [RowNum]");

            queryBuilder.ParameterValues.Count.ShouldBeEquivalentTo(0);
            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToListPageinatedWithCount()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToList(10, 20, out var _);

            executedQueries.Count.ShouldBeEquivalentTo(2);
            executedQueries.First().ShouldBeEquivalentTo(@"SELECT COUNT(*)
FROM [dbo].[Accounts]");

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM (
    SELECT *,
    ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
    FROM [dbo].[Accounts]
) ALIAS_GENERATED_1
WHERE ([RowNum] >= @_minrow)
AND ([RowNum] <= @_maxrow)
ORDER BY [RowNum]");

            queryBuilder.ParameterValues.Count.ShouldBeEquivalentTo(0);

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToList()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToList();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromStream()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.Stream();

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldNotModifyQueryBuilderStateFromToDictionary()
        {
            var queryBuilder = QueryBuilder("Accounts");

            queryBuilder.ToDictionary(d => d.GetHashCode().ToString());

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT *
FROM [dbo].[Accounts]
ORDER BY [Id]");

            queryBuilder.Take(1);

            LastExecutedQuery().ShouldBeEquivalentTo(@"SELECT TOP 1 *
FROM [dbo].[Accounts]
ORDER BY [Id]");
        }

        [Test]
        public void ShouldAddWhereClauseToExistingQueryBuilder()
        {
            transaction.Query<object>().Returns(TableQueryBuilder("Accounts"));

            var query = transaction.Query<object>();

            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            query.Where("Id", UnarySqlOperand.Equal, 1);
            
            // Old versions of Query returned a stateful object, while now each call is stateless - it returns a new object.
            // It's invalid to call a method twice
            Assert.Throws<Exception>(() => query.ToList());
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
            //TODO: Likely won't work
            return new TableSourceQueryBuilder<object>(tableName, "dbo", "Id", transaction, Substitute.For<CacheTableColumnsBuilder>(), TableAliasGenerator(), new UniqueParameterNameGenerator(), new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }
    }
}