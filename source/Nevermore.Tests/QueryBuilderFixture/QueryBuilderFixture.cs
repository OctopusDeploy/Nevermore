using System;
using System.Collections.Generic;
using Assent;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Querying.AST;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class QueryBuilderFixture
    {
        ITableAliasGenerator tableAliasGenerator;
        IUniqueParameterNameGenerator uniqueParameterNameGenerator;
        readonly IReadTransaction transaction = Substitute.For<IReadTransaction>();

        [SetUp]
        public void SetUp()
        {
            tableAliasGenerator = new TableAliasGenerator();
            uniqueParameterNameGenerator = new UniqueParameterNameGenerator();
            transaction.ClearReceivedCalls();
        }
        
        ITableSourceQueryBuilder<TDocument> CreateQueryBuilder<TDocument>(string tableName) where TDocument : class
        {
            return new TableSourceQueryBuilder<TDocument>(tableName, transaction, tableAliasGenerator, uniqueParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }
        
        [Test]
        public void ShouldGenerateSelect()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5")
                .OrderBy("Name")
                .DebugViewRawQuery();

            const string expected = @"SELECT *
FROM dbo.[Orders]
WHERE ([Price] > 5)
ORDER BY [Name]";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateSelectNoOrder()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5")
                .DebugViewRawQuery();

            const string expected = @"SELECT *
FROM dbo.[Orders]
WHERE ([Price] > 5)
ORDER BY [Id]";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateSelectForQueryBuilder()
        {
            var actual = CreateQueryBuilder<object>("Orders")
             .Where("[Price] > 5")
             .DebugViewRawQuery();

            const string expected = @"SELECT *
FROM dbo.[Orders]
WHERE ([Price] > 5)
ORDER BY [Id]";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateSelectForJoin()
        {
            var leftQueryBuilder = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<object>("Customers");

            var actual = leftQueryBuilder
                .InnerJoin(rightQueryBuilder)
                .On("CustomerId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateSelectForMultipleJoins()
        {

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders");
            var join1QueryBuilder = CreateQueryBuilder<object>("Customers");
            var join2QueryBuilder = CreateQueryBuilder<object>("Accounts");

            var actual = leftQueryBuilder
                .InnerJoin(join1QueryBuilder).On("CustomerId", JoinOperand.Equal, "Id")
                .InnerJoin(join2QueryBuilder).On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateSelectForChainedJoins()
        {

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders").Alias("Orders");
            var join1QueryBuilder = CreateQueryBuilder<object>("Customers").Alias("Customers");
            var join2QueryBuilder = CreateQueryBuilder<object>("Accounts").Alias("Accounts");

            var actual = leftQueryBuilder
                .InnerJoin(join1QueryBuilder).On("CustomerId", JoinOperand.Equal, "Id")
                .InnerJoin(join2QueryBuilder)
                    .On("Customers", "Id", JoinOperand.Equal, "CustomerId")
                    .On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }
        
        [Test]
        public void ShouldGenerateSelectForMultipleJoinsWithParameter()
        {

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders").Where("CustomerId", UnarySqlOperand.Equal, "customers-1");
            var join1QueryBuilder = CreateQueryBuilder<object>("Customers").Where("Name", UnarySqlOperand.Equal, "Abc");
            var join2QueryBuilder = CreateQueryBuilder<object>("Accounts").Where("Name", UnarySqlOperand.Equal, "CBA");

            var actual = leftQueryBuilder
                .InnerJoin(join1QueryBuilder.Subquery()).On("CustomerId", JoinOperand.Equal, "Id")
                .InnerJoin(join2QueryBuilder.Subquery()).On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateSelectForComplicatedSubqueryJoin()
        {
            var orders = CreateQueryBuilder<object>("Orders");
            var customers = CreateQueryBuilder<object>("Customers")
                .Where("IsActive = 1")
                .OrderBy("Created");

            var accounts = CreateQueryBuilder<object>("Accounts").Hint("WITH (UPDLOCK)");

            var actual = orders.InnerJoin(customers.Subquery())
                .On("CustomerId", JoinOperand.Equal, "Id")
                .On("Owner", JoinOperand.Equal, "Owner")
                .InnerJoin(accounts.Subquery()).On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldSuppressOrderByWhenGeneratingCount()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<object>("Orders")
                .OrderBy("Created")
                .Count();

            var expected = @"SELECT COUNT(*)
FROM dbo.[Orders]";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateCount()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<object>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .Count();

            var expected = @"SELECT COUNT(*)
FROM dbo.[Orders] NOLOCK
WHERE ([Price] > 5)";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateCountForQueryBuilder()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<object>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .Count();

            const string expected = @"SELECT COUNT(*)
FROM dbo.[Orders] NOLOCK
WHERE ([Price] > 5)";

            actual.Should().Be(expected);
        }

        [Test]
        public void ShouldGenerateCountForJoin()
        {
            string actual = null;
            transaction.ExecuteScalar<int>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<object>("Customers");
            leftQueryBuilder.InnerJoin(rightQueryBuilder).On("CustomerId", JoinOperand.Equal, "Id")
                .Count();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGeneratePaginate()
        {
            string actual = null;
            transaction.Stream<object>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());
            CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5")
                .OrderBy("Foo")
                .ToList(10, 20);
            
            this.Assent(actual);
        }


        [Test]
        public void ShouldGeneratePaginateForJoin()
        {
            string actual = null;
            transaction.Stream<object>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<object>("Customers");
            leftQueryBuilder
                .InnerJoin(rightQueryBuilder).On("CustomerId", JoinOperand.Equal, "Id")
                .ToList(10, 20);

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateTop()
        {
            string actual = null;
            transaction.Stream<object>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());
            CreateQueryBuilder<object>("Orders")
                .NoLock()
                .Where("[Price] > 5")
                .OrderBy("Id")
                .Take(100);

            var expected = @"SELECT TOP 100 *
FROM dbo.[Orders] NOLOCK
WHERE ([Price] > 5)
ORDER BY [Id]";

            actual.Should().Be(expected);
        }


        [Test]
        public void ShouldGenerateTopForJoin()
        {
            string actual = null;
            transaction.Stream<object>(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            var leftQueryBuilder = CreateQueryBuilder<object>("Orders")
                .Where("[Price] > 5");
            var rightQueryBuilder = CreateQueryBuilder<object>("Customers");

            leftQueryBuilder.InnerJoin(rightQueryBuilder).On("CustomerId", JoinOperand.Equal, "Id")
                .Take(100);

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateExpectedLikeParametersForQueryBuilder()
        {
            CommandParameterValues parameterValues = null;
            transaction.Stream<object>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            // We need to make sure parameters like opening square brackets are correctly escaped for LIKE pattern matching in SQL.
            var environment = new
            {
                Id = "Environments-1"
            };
            CreateQueryBuilder<object>("Project")
                .Where("[JSON] LIKE @jsonPatternSquareBracket")
                .LikeParameter("jsonPatternSquareBracket", $"\"AutoDeployReleaseOverrides\":[{{\"EnvironmentId\":\"{environment.Id}\"")
                .Where("[JSON] NOT LIKE @jsonPatternPercentage")
                .LikeParameter("jsonPatternPercentage", $"SomeNonExistantField > 5%")
                .ToList();

            var actualParameter1 = parameterValues["jsonPatternSquareBracket"];
            const string expectedParameter1 = "%\"AutoDeployReleaseOverrides\":[[]{\"EnvironmentId\":\"Environments-1\"%";
            actualParameter1.Should().Be(expectedParameter1);

            var actualParameter2 = parameterValues["jsonPatternPercentage"];
            const string expectedParameter2 = "%SomeNonExistantField > 5[%]%";
            actualParameter2.Should().Be(expectedParameter2);
        }

        [Test]
        public void ShouldGenerateExpectedPipedLikeParametersForQueryBuilder()
        {
            CommandParameterValues parameterValues = null;
            transaction.Stream<object>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            CreateQueryBuilder<object>("Project")
                .LikePipedParameter("Name", "Foo|Bar|Baz")
                .ToList();

            parameterValues["Name"].Should().Be("%|Foo|Bar|Baz|%");
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForAnyWithResults()
        {
            const string expectedSql = @"IF EXISTS(SELECT *
FROM dbo.[Todos]
WHERE ([Completed] < @completed))
    SELECT @true
ELSE
    SELECT @false";

            transaction.ClearReceivedCalls();

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Any();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => (int)cp["completed"] == 5
                                                     && (int) cp["true"] == 1
                                                     && (int) cp["false"] == 0));

            result.Should().Be(true);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForAnyWithNoResults()
        {
            const string expectedSql = @"IF EXISTS(SELECT *
FROM dbo.[Todos]
WHERE ([Completed] < @completed))
    SELECT @true
ELSE
    SELECT @false";

            transaction.ClearReceivedCalls();

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(0);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Any();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => (int)cp["completed"] == 5
                                                     && (int) cp["true"] == 1
                                                     && (int) cp["false"] == 0));

            result.Should().Be(false);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForAnyIgnoreOrderBy()
        {
            const string expectedSql = @"IF EXISTS(SELECT *
FROM dbo.[Todos]
WHERE ([Completed] < @completed))
    SELECT @true
ELSE
    SELECT @false";

            transaction.ClearReceivedCalls();

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(0);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .OrderBy("Completed")
                .Any();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => (int) cp["completed"] == 5
                                                     && (int) cp["true"] == 1
                                                     && (int) cp["false"] == 0));

            result.Should().Be(false);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThan()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] < @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(2);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(2);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] < @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(2);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", UnarySqlOperand.LessThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(2);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqual()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] <= @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(10);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] <= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(10);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqualExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] <= @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(10);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", UnarySqlOperand.LessThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(10);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereEquals()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] = @title)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] = @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereEqualsExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] = @title)";
            
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", UnarySqlOperand.Equal, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereNotEquals()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] <> @title)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] <> @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereNotEqualsExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] <> @title)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", UnarySqlOperand.NotEqual, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "nevermore"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThan()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] > @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(11);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] > @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(11);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] > @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(3);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", UnarySqlOperand.GreaterThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(3);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqual()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] >= @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(21);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] >= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(21);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqualExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] >= @completed)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(21);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", UnarySqlOperand.GreaterThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => int.Parse(cp["completed"].ToString()) == 5));

            result.Should().Be(21);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereContains()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] LIKE @title)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] LIKE @title")
                .Parameter("title", "%nevermore%")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "%nevermore%"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereContainsExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] LIKE @title)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where(t => t.Title.Contains("nevermore"))
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp["title"].ToString() == "%nevermore%"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereString()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] IN (@nevermore, @octofront))";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("[Title] IN (@nevermore, @octofront)")
                .Parameter("nevermore", "nevermore")
                .Parameter("octofront", "octofront")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    cp["nevermore"].ToString() == "nevermore"
                    && cp["octofront"].ToString() == "octofront"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereArray()
        {
            const string expectedSql = @"SELECT *
FROM dbo.[Project]
WHERE ([State] IN (@state1, @state2))
ORDER BY [Id]";

            var queryBuilder = CreateQueryBuilder<object>("Project")
                .Where("State", ArraySqlOperand.In, new[] { State.Queued, State.Running });

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereList()
        {
            var matches = new List<State>
            {
                State.Queued,
                State.Running
            };
            const string expectedSql = @"SELECT *
FROM dbo.[Project]
WHERE ([State] IN (@state1, @state2))
ORDER BY [Id]";
            var queryBuilder = CreateQueryBuilder<object>("Project")
                .Where("State", ArraySqlOperand.In, matches);

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInUsingEmptyList()
        {
            const string expextedSql = @"SELECT *
FROM dbo.[Project]
WHERE (0 = 1)
ORDER BY [Id]";

            var queryBuilder =
                CreateQueryBuilder<object>("Project").Where("State", ArraySqlOperand.In, new List<State>());

            queryBuilder.DebugViewRawQuery().Should().Be(expextedSql);
        }


        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[TodoItem]
WHERE ([Title] IN (@title1, @title2))";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .Where("Title", ArraySqlOperand.In, new[] { "nevermore", "octofront" })
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    cp["title1"].ToString() == "nevermore"
                    && cp["title2"].ToString() == "octofront"));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetween()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

#pragma warning disable NV0006
            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] BETWEEN @startvalue AND @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();
#pragma warning restore NV0006

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("Completed", BinarySqlOperand.Between, 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqual()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

#pragma warning disable NV0006
            var result = CreateQueryBuilder<Todos>("Todos")
                .Where("[Completed] >= @startvalue AND [Completed] <= @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();
#pragma warning restore NV0006

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqualExtension()
        {
            const string expectedSql = @"SELECT COUNT(*)
FROM dbo.[Todos]
WHERE ([Completed] >= @startvalue)
AND ([Completed] <= @endvalue)";

            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(1);

            var result = CreateQueryBuilder<Todos>("Todos")
                .WhereBetweenOrEqual("Completed", 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            result.Should().Be(1);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForOrderBy()
        {
            const string expectedSql = @"SELECT TOP 1 *
FROM dbo.[TodoItem]
ORDER BY [Title]";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.Stream<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(new[] { todoItem });

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .OrderBy("Title")
                .FirstOrDefault();

            transaction.Received(1).Stream<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp.Count == 0));

            Assert.NotNull(result);
            result.Should().Be(todoItem);
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForOrderByDescending()
        {
            const string expectedSql = @"SELECT TOP 1 *
FROM dbo.[TodoItem]
ORDER BY [Title] DESC";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.Stream<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameterValues>())
                .Returns(new[] { todoItem });

            var result = CreateQueryBuilder<TodoItem>("TodoItem")
                .OrderByDescending("Title")
                .FirstOrDefault();

            transaction.Received(1).Stream<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameterValues>(cp => cp.Count == 0));

            Assert.NotNull(result);
            result.Should().Be(todoItem);
        }

        [Test]
        public void ShouldGenerateAliasForTable()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Alias("ORD")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateAliasForSubquery()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Subquery()
                .Alias("ORD")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateAliasesForSourcesInJoin()
        {
            var accounts = CreateQueryBuilder<object>("Accounts")
                .Subquery()
                .Alias("ACC");

            var actual = CreateQueryBuilder<object>("Orders")
                .Alias("ORD")
                .InnerJoin(accounts)
                .On("AccountId", JoinOperand.Equal, "Id")
                .Where("Id", UnarySqlOperand.Equal, 1)
                .OrderBy("Name")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateColumnSelection()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Column("Foo")
                .Column("Bar")
                .Column("Baz")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateColumnSelectionWithAliases()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Column("Foo", "F")
                .Column("Bar", "B")
                .Column("Baz", "B2")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateColumnSelectionWithTableAlias()
        {
            const string ordersTableAlias = "ORD";
            var actual = CreateQueryBuilder<object>("Orders")
                .Alias(ordersTableAlias)
                .Column("Foo", "F", ordersTableAlias)
                .Column("Bar", "B", ordersTableAlias)
                .Column("Baz", "B2", ordersTableAlias)
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateColumnSelectionForJoin()
        {
            var accounts = CreateQueryBuilder<object>("Accounts")
                .Subquery()
                .Alias("ACC");

            var actual = CreateQueryBuilder<object>("Orders")
                .Alias("ORD")
                .InnerJoin(accounts)
                .On("AccountId", JoinOperand.Equal, "Id")
                .Column("Id", "OrderId", "ORD")
                .Column("Id", "AccountId", "Acc")
                .Column("Number") // should come from "ORD"
                .Column("Id", "OrderId2") // should come from "ORD"
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateRowNumber()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .AddRowNumberColumn("ROWNUM")
                .OrderBy("ROWNUM")
                .DebugViewRawQuery();

            this.Assent(actual);
        }
        
        [Test]
        public void Replace_Release_LatestByProjectChannel()
        {
            var actual = CreateQueryBuilder<object>("Release")
                .AllColumns()
                .OrderByDescending("Assembled")
                .AddRowNumberColumn("RowNum", "SpaceId", "ProjectId", "ChannelId")
                .Subquery()
                .Alias("rs")
                .Where("RowNum", UnarySqlOperand.Equal, 1)
                .DebugViewRawQuery();

            this.Assent(actual);
        }
        
        [Test]
        public void Replace_LatestSuccessfulDeployments()
        {
            const string eventAlias = "e";
            const string eventRelatedDocumentAlias = "eventRelatedDocuments";

            const string eventOccurred = "occurred";
            const string eventCategory = "category";

            var eventRelatedDocuments = CreateQueryBuilder<object>("EventRelatedDocument").Alias(eventRelatedDocumentAlias);
            var eventJoin = CreateQueryBuilder<object>("Event").Alias(eventAlias);

            var actual = CreateQueryBuilder<object>("Deployment")
                .Alias("deployments")
                
                .InnerJoin(eventRelatedDocuments)
                .On("Id", JoinOperand.Equal, "RelatedDocumentId")

                .InnerJoin(eventJoin)
                .On(eventRelatedDocumentAlias, "EventId", JoinOperand.Equal, "Id")

                .AllColumns()
                .OrderByDescending(eventOccurred, eventAlias)
                .AddRowNumberColumn("Rank", "EnvironmentId", "ProjectId", "TenantId")
                .Where($"{eventAlias}.{eventCategory} = \'DeploymentSucceeded\'")
                .Subquery()
                .Alias("d")
                .Where("Rank", UnarySqlOperand.Equal, 1)
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateRowNumberWithOrderBy()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateRowNumberWithPartitionBy()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", "Region", "Area")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateRowNumberWithPartitionByInJoin()
        {
            var account = CreateQueryBuilder<object>("Account");
            var actual = CreateQueryBuilder<object>("Orders")
                .InnerJoin(account)
                .On("AccountId", JoinOperand.Equal, "Id")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", "Region", "Area")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateRowNumberWithPartitionByInJoinWithCustomAliases()
        {
            var account = CreateQueryBuilder<object>("Account")
                .Alias("ACC");
            var actual = CreateQueryBuilder<object>("Orders")
                .Alias("ORD")
                .InnerJoin(account)
                .On("AccountId", JoinOperand.Equal, "Id")
                .OrderBy("Foo")
                .AddRowNumberColumn("ROWNUM", new ColumnFromTable("Region", "ACC"), new ColumnFromTable("Area", "ACC"))
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateUnion()
        {
            var account = CreateQueryBuilder<object>("Account")
                .Column("Id", "Id");
            var actual = CreateQueryBuilder<object>("Orders")
                .Column("Id", "Id")
                .Union(account)
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateMultipleUnionsWithoutNesting()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .Column("Id", "Id")
                .Union(CreateQueryBuilder<object>("Account")
                    .Column("Id", "Id"))
                .Union(CreateQueryBuilder<object>("Customers")
                    .Column("Id", "Id"))
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldCollectParameterValuesFromUnion()
        {
            CommandParameterValues parameterValues = null;
            transaction.Stream<object>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            var account = CreateQueryBuilder<object>("Account")
                .Where("Name", UnarySqlOperand.Equal, "ABC")
                .Column("Id", "Id");
            CreateQueryBuilder<object>("Orders")
                .Column("Id", "Id")
                .Union(account)
                .ToList();

            parameterValues.Should().Contain("Name", "ABC");
        }

        [Test]
        public void ShouldCollectParametersAndDefaultsFromUnion()
        {
            var parameter = new Parameter("Name", new NVarCharMax());
            var account = CreateQueryBuilder<object>("Account")
                .WhereParameterized("Name", UnarySqlOperand.Equal, parameter)
                .ParameterDefault("ABC")
                .Column("Id", "Id");
            var query = CreateQueryBuilder<object>("Orders")
                .Column("Id", "Id")
                .Union(account);

            this.Assent(query.AsStoredProcedure("ShouldCollectParametersAndDefaultsFromUnion"));
        }

        [Test]
        public void ShouldGenerateCalculatedColumn()
        {
            var actual = CreateQueryBuilder<object>("Orders")
                .CalculatedColumn("'CONSTANT'", "MyConstant")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateLeftHashJoin()
        {
            var account = CreateQueryBuilder<object>("Account");
            var actual = CreateQueryBuilder<object>("Orders")
                .LeftHashJoin(account)
                .On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateWithMultipleLeftHashJoinsSubQueryWithPrameter()
        {
            var account = CreateQueryBuilder<object>("Account").Where("Name", UnarySqlOperand.Equal, "Octopus 1");
            var company = CreateQueryBuilder<object>("Company").Where("Name", UnarySqlOperand.Equal, "Octopus 2");
            var actual = CreateQueryBuilder<object>("Orders")
                .LeftHashJoin(account.Subquery())
                .On("AccountId", JoinOperand.Equal, "Id")
                .LeftHashJoin(company.Subquery())
                .On("CompanyId", JoinOperand.Equal, "CompanyId")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateWithMultipleLeftHashJoinsWithTableAndSubQueryWithPrameter()
        {
            var account = CreateQueryBuilder<object>("Account");
            var company = CreateQueryBuilder<object>("Company").Where("Name", UnarySqlOperand.Equal, "Octopus");
            var actual = CreateQueryBuilder<object>("Orders")
                .LeftHashJoin(account)
                .On("AccountId", JoinOperand.Equal, "Id")
                .LeftHashJoin(company.Subquery())
                .On("CompanyId", JoinOperand.Equal, "CompanyId")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        [Test]
        public void ShouldGenerateMultipleJoinTypes()
        {
            var customers = CreateQueryBuilder<Customer>("Customers")
                .Where(c => c.Name.StartsWith("Bob"));
            var account = CreateQueryBuilder<object>("Account");
            var actual = CreateQueryBuilder<object>("Orders")
                .InnerJoin(customers.AsType<object>().Subquery())
                .On("CustomerId", JoinOperand.Equal, "Id")
                .LeftHashJoin(account)
                .On("AccountId", JoinOperand.Equal, "Id")
                .DebugViewRawQuery();

            this.Assent(actual);
        }

        class Customer
        {
            public string Name { get; set; }
        }

        [Test]
        public void ShouldGenerateComplexDashboardView()
        {
            const string taskTableAlias = "t";
            const string releaseTableAlias = "r";
            var dashboard = CurrentDeployments()
                .Union(PreviousDeployments())
                .Alias("d")
                .InnerJoin(CreateQueryBuilder<object>("ServerTask").Alias(taskTableAlias))
                .On("TaskId", JoinOperand.Equal, "Id")
                .InnerJoin(CreateQueryBuilder<object>("Release").Alias(releaseTableAlias))
                .On("ReleaseId", JoinOperand.Equal, "Id")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironmentId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .Column("CurrentOrPrevious", "CurrentOrPrevious")
                .Column("State", "State", taskTableAlias)
                .Column("HasPendingInterruptions", "HasPendingInterruptions", taskTableAlias)
                .Column("HasWarningsOrErrors", "HasWarningsOrErrors", taskTableAlias)
                .Column("ErrorMessage", "ErrorMessage", taskTableAlias)
                .Column("QueueTime", "QueueTime", taskTableAlias)
                .Column("CompletedTime", "CompletedTime", taskTableAlias)
                .Column("Version", "Version", releaseTableAlias)
                .Where("([Rank]=1 AND CurrentOrPrevious='P') OR ([Rank]=1 AND CurrentOrPrevious='C')");

            var actual = dashboard.AsView("Dashboard");

            this.Assent(actual);
        }

        IQueryBuilder<object> CurrentDeployments()
        {
            const string taskTableAlias = "t";
            const string deploymentTableAlias = "d";

            return CreateQueryBuilder<object>("Deployment")
                .Alias(deploymentTableAlias)
                .InnerJoin(CreateQueryBuilder<object>("ServerTask").Alias(taskTableAlias))
                .On("TaskId", JoinOperand.Equal, "Id")
                .CalculatedColumn("'C'", "CurrentOrPrevious")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironmentId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias), new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"NOT (({taskTableAlias}.State = \'Canceled\' OR {taskTableAlias}.State = \'Cancelling\') AND {taskTableAlias}.StartTime IS NULL)");
        }
        
        IQueryBuilder<object> PreviousDeployments()
        {
            const string deploymentTableAlias = "d";
            const string taskTableAlias = "t";
            const string l = "l";
            return CreateQueryBuilder<object>("Deployment")
                .Alias(deploymentTableAlias)
                .InnerJoin(CreateQueryBuilder<object>("ServerTask").Alias(taskTableAlias))
                .On("TaskId", JoinOperand.Equal, "Id")
                .LeftHashJoin(LQuery().Subquery().Alias(l))
                .On("Id", JoinOperand.Equal, "Id")
                .CalculatedColumn("'P'", "CurrentOrPrevious")
                .Column("Id", "Id")
                .Column("Created", "Created")
                .Column("ProjectId", "ProjectId")
                .Column("EnvironemntId", "EnvironmentId")
                .Column("ReleaseId", "ReleaseId")
                .Column("TaskId", "TaskId")
                .Column("ChannelId", "ChannelId")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias),
                    new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"{taskTableAlias}.State = 'Success'")
                .Where($"{l}.Id is null");
        }

        IQueryBuilder<object> LQuery()
        {
            return LatestDeployment()
                .Subquery()
                .Alias("LatestDeployment")
                .Column("Id")
                .Where("Rank", UnarySqlOperand.Equal, 1);
        } 
        
        IQueryBuilder<object> LatestDeployment()
        {
            var deploymentTableAlias = "d";
            var serverTaskTableAlias = "t";
            return CreateQueryBuilder<object>("Deployment")
                .Alias(deploymentTableAlias)
                .InnerJoin(CreateQueryBuilder<object>("ServerTask").Alias(serverTaskTableAlias))
                .On("TaskId", JoinOperand.Equal, "Id")
                .Column("Id", "Id")
                .OrderByDescending("Created")
                .AddRowNumberColumn("Rank", new ColumnFromTable("EnvironmentId", deploymentTableAlias), new ColumnFromTable("ProjectId", deploymentTableAlias))
                .Where($"NOT (({serverTaskTableAlias}.State = \'Canceled\' OR {serverTaskTableAlias}.State = \'Cancelling\') AND {serverTaskTableAlias}.StartTime IS NULL)");
        }

        [Test]
        public void ShouldGenerateComplexStoredProcedureWithParameters()
        {
            const string eventTableAlias = "Event";

            var withJoins = CreateQueryBuilder<object>("Deployment")
                .InnerJoin(CreateQueryBuilder<object>("DeploymentRelatedMachine"))
                .On("Id", JoinOperand.Equal, "DeploymentId")
                .InnerJoin(CreateQueryBuilder<object>("EventRelatedDocument"))
                .On("Id", JoinOperand.Equal, "RelatedDocumentId")
                .InnerJoin(CreateQueryBuilder<object>("Event").Alias(eventTableAlias))
                .On("Id", JoinOperand.Equal, "EventId")
                .AllColumns()
                .OrderByDescending($"[{eventTableAlias}].[Occurred]")
                .AddRowNumberColumn("Rank", "EnvironmentId", "ProjectId", "TenantId");

            var actual = withJoins
                .Where("[DeploymentRelatedMachine].MachineId = @machineId")
                .Parameter(new Parameter("machineId", new NVarCharMax()))
                .Where("[Event].Category = \'DeploymentSucceeded\'")
                .Subquery()
                .Where("Rank = 1");

            this.Assent(actual.AsStoredProcedure("LatestSuccessfulDeploymentsToMachine"));
        }

        [Test]
        public void ShouldGenerateFunctionWithParameters()
        {
            var packagesQuery = CreateQueryBuilder<object>("NuGetPackages")
                .Where("PackageId = @packageid")
                .Parameter(new Parameter("packageid", new NVarChar(250)));

            this.Assent(packagesQuery.AsFunction("PackagesMatchingId"));
        }

        [Test]
        public void ShouldGenerateStoredProcWithDefaultValues()
        {
            var packageIdParameter = new Parameter("packageid", new NVarChar(250));
            var query = CreateQueryBuilder<object>("NuGetPackages")
                .Where("(@packageid is '') or (PackageId = @packageid)")
                .Parameter(packageIdParameter)
                .ParameterDefault(packageIdParameter, "");

            this.Assent(query.AsStoredProcedure("PackagesMatchingId"));
        }

        [Test]
        public void ShouldGenerateFunctionWithDefaultValues()
        {
            var packageIdParameter = new Parameter("packageid", new NVarChar(250));
            var query = CreateQueryBuilder<object>("NuGetPackages")
                .Where("(@packageid is '') or (PackageId = @packageid)")
                .Parameter(packageIdParameter)
                .ParameterDefault(packageIdParameter, "");

            this.Assent(query.AsFunction("PackagesMatchingId"));
        }

        [Test]
        public void ShouldCollectParameterValuesFromSubqueriesInJoin()
        {
            CommandParameterValues parameterValues = null;
            transaction.Stream<object>(Arg.Any<string>(), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            var query = CreateQueryBuilder<object>("Orders")
                .InnerJoin(CreateQueryBuilder<object>("Customers")
                    .Where("Name", UnarySqlOperand.Equal, "Bob")
                    .Subquery())
                .On("CustomerId", JoinOperand.Equal, "Id");

            query.ToList();

            parameterValues.Should().Contain("Name", "Bob");
        }

        [Test]
        public void ShouldCollectParametersAndDefaultsFromSubqueriesInJoin()
        {
            var parameter = new Parameter("Name", new NVarCharMax());
            var query = CreateQueryBuilder<object>("Orders")
                .InnerJoin(CreateQueryBuilder<object>("Customers")
                    .WhereParameterized("Name", UnarySqlOperand.Equal, parameter)
                    .ParameterDefault("Bob")
                    .Subquery())
                .On("CustomerId", JoinOperand.Equal, "Id");

            this.Assent(query.AsStoredProcedure("ShouldCollectParametersFromSubqueriesInJoin"));
        }

        [Test]
        public void ShouldAddPaginatedQueryParameters()
        {
            CommandParameterValues parameterValues = null;
            string query = null;
            transaction.Stream<object>(Arg.Do<string>(q => query = q), Arg.Do<CommandParameterValues>(pv => parameterValues = pv));

            CreateQueryBuilder<object>("Orders")
                .Where("Id", UnarySqlOperand.Equal, "1")
                .ToList(10, 20);

            query.ShouldBeEquivalentTo(@"SELECT *
FROM (
    SELECT *,
    ROW_NUMBER() OVER (ORDER BY [Id]) AS RowNum
    FROM dbo.[Orders]
    WHERE ([Id] = @id)
) ALIAS_GENERATED_1
WHERE ([RowNum] >= @_minrow)
AND ([RowNum] <= @_maxrow)
ORDER BY [RowNum]");

            parameterValues.Count.ShouldBeEquivalentTo(3);
            parameterValues["id"].ShouldBeEquivalentTo("1");
            parameterValues["_minrow"].ShouldBeEquivalentTo(11);
            parameterValues["_maxrow"].ShouldBeEquivalentTo(30);
        }

        [Test]
        public void ShouldGenerateUniqueParameterNames()
        {
            string actual = null;
            CommandParameterValues parameters = null;
            transaction.Stream<TodoItem>(Arg.Do<string>(s => actual = s),
                Arg.Do<CommandParameterValues>(p => parameters = p));

            var earlyDate = DateTime.Now;
            var laterDate = earlyDate + TimeSpan.FromDays(1);
            var query = CreateQueryBuilder<TodoItem>("Todos")
                .Where("AddedDate", UnarySqlOperand.GreaterThan, earlyDate)
                .Where("AddedDate", UnarySqlOperand.LessThan, laterDate);

            query.FirstOrDefault();

            const string expected = @"SELECT TOP 1 *
FROM dbo.[Todos]
WHERE ([AddedDate] > @addeddate)
AND ([AddedDate] < @addeddate_1)
ORDER BY [Id]";

            parameters.Values.Count.ShouldBeEquivalentTo(2);
            parameters["addeddate"].ShouldBeEquivalentTo(earlyDate);
            parameters["addeddate_1"].ShouldBeEquivalentTo(laterDate);

            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void ShouldGenerateUniqueParameterNamesInJoin()
        {
            string actual = null;
            CommandParameterValues parameters = null;
            transaction.Stream<TodoItem>(Arg.Do<string>(s => actual = s),
                Arg.Do<CommandParameterValues>(p => parameters = p));

            var createdDate = DateTime.Now;
            var joinDate = createdDate - TimeSpan.FromDays(1);
            var sharedFieldName = "Date";

            var orders = CreateQueryBuilder<object>("Orders")
                .Where(sharedFieldName, UnarySqlOperand.Equal, createdDate);


            var query = CreateQueryBuilder<TodoItem>("Customer")
                .Where(sharedFieldName, UnarySqlOperand.Equal, joinDate)
                .InnerJoin(orders.Subquery())
                .On("Id", JoinOperand.Equal, "CustomerId");

            query.FirstOrDefault();

            const string expected =
                @"SELECT TOP 1 ALIAS_GENERATED_2.*
FROM (
    SELECT *
    FROM dbo.[Customer]
    WHERE ([Date] = @date_1)
) ALIAS_GENERATED_2
INNER JOIN (
    SELECT *
    FROM dbo.[Orders]
    WHERE ([Date] = @date)
) ALIAS_GENERATED_1
ON ALIAS_GENERATED_2.[Id] = ALIAS_GENERATED_1.[CustomerId]
ORDER BY ALIAS_GENERATED_2.[Id]";

            parameters.Values.Count.ShouldBeEquivalentTo(2);
            parameters["date"].ShouldBeEquivalentTo(createdDate);
            parameters["date_1"].ShouldBeEquivalentTo(joinDate);

            actual.ShouldBeEquivalentTo(expected);
        }

        [Test]
        public void ShouldThrowIfDifferentNumberOfParameterValuesProvided()
        {
            CreateQueryBuilder<object>("Todo")
                .WhereParameterized("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterValues(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }

        [Test]
        public void ShouldThrowIfDifferentNumberOfParameterDefaultsProvided()
        {
            CreateQueryBuilder<object>("Todo")
                .WhereParameterized("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterDefaults(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }

        public class Customer2
        {
            public string Name { get; set; }
        }

        [Test]
        public void MultipleParametersInLinqQuery()
        {
            string actual = null;
            CommandParameterValues parameters = null;
            transaction.Stream<Customer2>(Arg.Do<string>(s => actual = s),
                Arg.Do<CommandParameterValues>(p => parameters = p));

            CreateQueryBuilder<Customer2>("Customers")
                .Where(d => d.Name != "Alice" && d.Name != "Bob")
                .ToList();

            const string expected = @"SELECT *
FROM dbo.[Customers]
WHERE ([Name] <> @name)
AND ([Name] <> @name_1)
ORDER BY [Id]";

            actual.Should().BeEquivalentTo(expected);
            parameters.Count.ShouldBeEquivalentTo(2);
            parameters.Should().Contain("name", "Alice");
            parameters.Should().Contain("name_1", "Bob");
        }

        [Test]
        public void ShouldGenerateSubqueryWhenThereAreNoCustomizations()
        {
            var subquerySql = CreateQueryBuilder<object>("Accounts")
                .Subquery()
                .GetSelectBuilder()
                .GenerateSelectWithoutDefaultOrderBy()
                .GenerateSql();

            this.Assent(subquerySql);
        }
    }

    public class Todos
    {
        public int Id { get; set; }
        public int Completed { get; set; }
        public List<TodoItem> Items { get; set; }
    }

    public class TodoItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool Completed { get; set; }
    }

    public enum State
    {
        Queued,
        Running
    }
}
