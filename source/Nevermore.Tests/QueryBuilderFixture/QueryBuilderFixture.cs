using System.Collections.Generic;
using Assent;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Joins;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class QueryBuilderFixture
    {
        readonly ITableAliasGenerator tableAliasGenerator = Substitute.For<ITableAliasGenerator>();
        readonly IRelationalTransaction transaction;

        public QueryBuilderFixture()
        {
            transaction = Substitute.For<IRelationalTransaction>();

            var tableNumber = 0;
            tableAliasGenerator.GenerateTableAlias(Arg.Any<string>()).Returns(delegate
            {
                tableNumber++;
                return "t" + tableNumber;
            });
        }

        [Fact]
        public void ShouldGenerateSelect()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5")
                .OrderBy("Id")
                .DebugViewRawQuery();

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectNoOrder()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5")
                .QueryGenerator
                .SelectQuery(false);

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectForQueryBuilder()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
             .Where("[Price] > 5")
             .DebugViewRawQuery();

            const string expected = "SELECT * FROM dbo.[Orders] WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateSelectForJoin()
        {

            var leftQueryBuilder = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5");
            var rightQueryBuilder = new QueryBuilder<IDocument>(transaction, "Customers");
            var join = new Join(JoinType.InnerJoin, rightQueryBuilder.QueryGenerator)
                .On("CustomerId", JoinOperand.Equal, "Id");

            leftQueryBuilder.Join(join);

            var actual = leftQueryBuilder.DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateSelectForMultipleJoins()
        {

            var leftQueryBuilder = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator);
            var join1QueryBuilder = new QueryBuilder<IDocument>(transaction, "Customers");
            var join2QueryBuilder = new QueryBuilder<IDocument>(transaction, "Accounts");

            leftQueryBuilder.Join(
                    new Join(JoinType.InnerJoin, join1QueryBuilder.QueryGenerator)
                        .On("CustomerId", JoinOperand.Equal, "Id")
                )
                .Join(
                    new Join(JoinType.InnerJoin, join2QueryBuilder.QueryGenerator)
                        .On("AccountId", JoinOperand.Equal, "Id")
                );

            var actual = leftQueryBuilder.DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateSelectForComplicatedSubqueryJoin()
        {
            var orders = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator);
            var customers = new QueryBuilder<IDocument>(transaction, "Customers")
                                            .Where("IsActive = 1")
                                            .OrderBy("Id");

            var accounts = new QueryBuilder<IDocument>(transaction, "Accounts").Hint("WITH (UPDLOCK)");


            orders.Join(
                    new Join(JoinType.InnerJoin, customers.QueryGenerator)
                        .On("CustomerId", JoinOperand.Equal, "Id")
                        .On("Owner", JoinOperand.Equal, "Owner")
                )
                .Join(
                    new Join(JoinType.InnerJoin, accounts.QueryGenerator)
                        .On("AccountId", JoinOperand.Equal, "Id")
                );

            var actual = orders.DebugViewRawQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateCount()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5")
                .NoLock()
                .QueryGenerator
                .CountQuery();

            var expected = "SELECT COUNT(*) FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ShouldGenerateDelete()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5")
                .NoLock()
                .QueryGenerator
                .DeleteQuery();

            var expected = "DELETE FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateCountForQueryBuilder()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .NoLock()
                .Where("[Price] > 5")
                .QueryGenerator
                .CountQuery();

            const string expected = "SELECT COUNT(*) FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5)";

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ShouldGenerateCountForJoin()
        {
            var leftQueryBuilder = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5");
            var rightQueryBuilder = new QueryBuilder<IDocument>(transaction, "Customers");
            var join = new Join(JoinType.InnerJoin, rightQueryBuilder.QueryGenerator)
                .On("CustomerId", JoinOperand.Equal, "Id");
            leftQueryBuilder.Join(join);

            var actual = leftQueryBuilder.QueryGenerator.CountQuery();

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGeneratePaginate()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5")
                .OrderBy("Foo")
                .QueryGenerator
                .PaginateQuery(10,20);

            this.Assent(actual);
        }


        [Fact]
        public void ShouldGeneratePaginateForJoin()
        {

            var leftQueryBuilder = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5");
            var rightQueryBuilder = new QueryBuilder<IDocument>(transaction, "Customers");
            var join = new Join(JoinType.InnerJoin, rightQueryBuilder.QueryGenerator)
                .On("CustomerId", JoinOperand.Equal, "Id");
            leftQueryBuilder.Join(join);

            var actual = leftQueryBuilder.QueryGenerator.PaginateQuery(10,20);

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateTop()
        {
            var actual = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                                .Where("[Price] > 5")
                                .OrderBy("Id")
                                .NoLock()
                                .QueryGenerator
                                .TopQuery(100);

            var expected = "SELECT TOP 100 * FROM dbo.[Orders] NOLOCK WHERE ([Price] > 5) ORDER BY [Id]";

            Assert.Equal(expected, actual);
        }


        [Fact]
        public void ShouldGenerateTopForJoin()
        {

            var leftQueryBuilder = new QueryBuilder<IDocument>(transaction, "Orders", tableAliasGenerator)
                .Where("[Price] > 5");
            var rightQueryBuilder = new QueryBuilder<IDocument>(transaction, "Customers");
            var join = new Join(JoinType.InnerJoin, rightQueryBuilder.QueryGenerator)
                .On("CustomerId", JoinOperand.Equal, "Id");
            leftQueryBuilder.Join(join);

            var actual = leftQueryBuilder.QueryGenerator.TopQuery(100);

            this.Assent(actual);
        }

        [Fact]
        public void ShouldGenerateExpectedLikeParametersForQueryBuilder()
        {
            // We need to make sure parameters like opening square brackets are correctly escaped for LIKE pattern matching in SQL.
            var environment = new
            {
                Id = "Environments-1"
            };
            var queryBuilder = new QueryBuilder<IDocument>(transaction, "Project")
                .Where("[JSON] LIKE @jsonPatternSquareBracket")
                .LikeParameter("jsonPatternSquareBracket", $"\"AutoDeployReleaseOverrides\":[{{\"EnvironmentId\":\"{environment.Id}\"")
                .Where("[JSON] NOT LIKE @jsonPatternPercentage")
                .LikeParameter("jsonPatternPercentage", $"SomeNonExistantField > 5%");

            var actualParameter1 = queryBuilder.QueryGenerator.QueryParameters["jsonPatternSquareBracket"];
            const string expectedParameter1 = "%\"AutoDeployReleaseOverrides\":[[]{\"EnvironmentId\":\"Environments-1\"%";
            Assert.Equal(actualParameter1, expectedParameter1);

            var actualParameter2 = queryBuilder.QueryGenerator.QueryParameters["jsonPatternPercentage"];
            const string expectedParameter2 = "%SomeNonExistantField > 5[%]%";
            Assert.Equal(actualParameter2, expectedParameter2);
        }

        [Fact]
        public void ShouldGenerateExpectedPipedLikeParametersForQueryBuilder()
        {

            var queryBuilder = new QueryBuilder<IDocument>(transaction, "Project")
                .LikePipedParameter("Name", "Foo|Bar|Baz");

            Assert.Equal("%|Foo|Bar|Baz|%", queryBuilder.QueryGenerator.QueryParameters["Name"]);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] < @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(2);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(2, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] < @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(2);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.LessThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(2, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] <= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(10);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] <= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(10, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] <= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(10);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.LessThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(10, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereEquals()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] = @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] = @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereEqualsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] = @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.Equal, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereNotEquals()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] <> @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] <> @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereNotEqualsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] <> @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.NotEqual, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] > @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(11);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] > @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(11, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] > @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(3);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.GreaterThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(3, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(21);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] >= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(21, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @completed)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(21);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.GreaterThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.Equal(21, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereContains()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] LIKE @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] LIKE @title")
                .Parameter("title", "%nevermore%")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereContainsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] LIKE @title)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.Contains, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereString()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] IN (@nevermore, @octofront))";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] IN (@nevermore, @octofront)")
                .Parameter("nevermore", "nevermore")
                .Parameter("octofront", "octofront")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    cp["nevermore"].ToString() == "nevermore"
                    && cp["octofront"].ToString() == "octofront"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereArray()
        {
            const string expectedSql = "SELECT * FROM dbo.[Project] WHERE ([State] IN (@queued, @running)) ORDER BY [Id]";
            var queryBuilder = new QueryBuilder<IDocument>(transaction, "Project")
                .Where("State", SqlOperand.In, new[] {State.Queued, State.Running });

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInUsingWhereList()
        {
            var matches = new List<State>
            {
                State.Queued,
                State.Running
            };
            const string expectedSql = "SELECT * FROM dbo.[Project] WHERE ([State] IN (@queued, @running)) ORDER BY [Id]";
            var queryBuilder = new QueryBuilder<IDocument>(transaction, "Project")
                .Where("State", SqlOperand.In, matches);

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInWithSpaces()
        {
            const string expectedSql = "SELECT * FROM dbo.[Project] WHERE ([Foo] IN (@bar_1, @bar_2)) ORDER BY [Id]";

            var queryBuilder = new QueryBuilder<IDocument>(transaction, "Project")
                .Where("Foo", SqlOperand.In, new[] { "Bar 1", "Bar 2" });

            queryBuilder.DebugViewRawQuery().Should().Be(expectedSql);
        }


        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereInExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem] WHERE ([Title] IN (@nevermore, @octofront))";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.In, new[] { "nevermore", "octofront" })
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    cp["nevermore"].ToString() == "nevermore"
                    && cp["octofront"].ToString() == "octofront"));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetween()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] BETWEEN @startvalue AND @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.Between, 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] >= @startvalue AND [Completed] <= @endvalue")
                .Parameter("StartValue", 5)
                .Parameter("EndValue", 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos] WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";


            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.BetweenOrEqual, 5, 10)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp =>
                    int.Parse(cp["startvalue"].ToString()) == 5 &&
                    int.Parse(cp["endvalue"].ToString()) == 10));

            Assert.Equal(1, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForOrderBy()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem] ORDER BY [Title]";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(new[] { todoItem });

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .OrderBy("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp.Count == 0));

            Assert.NotNull(result);
            Assert.Equal(todoItem, result);
        }

        [Fact]
        public void ShouldGetCorrectSqlQueryForOrderByDescending()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem] ORDER BY [Title] DESC";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };

            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(new[] { todoItem });

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .OrderByDescending("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp.Count == 0));

            Assert.NotNull(result);
            Assert.Equal(todoItem, result);
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
