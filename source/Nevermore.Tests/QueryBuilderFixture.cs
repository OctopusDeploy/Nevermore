using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests
{
    [TestFixture]
    public class QueryBuilderFixture
    {
        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] < @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(2);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] < @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] < @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(2);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.LessThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(2));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] <= @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(10);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] <= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereLessThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] <= @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(10);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.LessThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereEquals()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] = @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] = @title")
                .Parameter("title", "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereEqualsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] = @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.Equal, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "nevermore"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThan()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] > @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(11);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] > @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(11));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] > @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(3);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.GreaterThan, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(3));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] >= @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(21);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("[Completed] >= @completed")
                .Parameter("completed", 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(21));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereGreaterThanOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] >= @completed)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(21);

            var result = new QueryBuilder<Todos>(transaction, "Todos")
                .Where("Completed", SqlOperand.GreaterThanOrEqual, 5)
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => int.Parse(cp["completed"].ToString()) == 5));

            Assert.That(result, Is.EqualTo(21));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereContains()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] LIKE @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] LIKE @title")
                .Parameter("title", "%nevermore%")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereContainsExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] LIKE @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.Contains, "nevermore")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "%nevermore%"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereIn()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] IN @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("[Title] IN @title")
                .Parameter("title", "(nevermore, octofront)")
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "(nevermore, octofront)"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereInExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[TodoItem]  WHERE ([Title] IN @title)";

            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteScalar<int>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(1);

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .Where("Title", SqlOperand.In, new [] {"nevermore", "octofront"})
                .Count();

            transaction.Received(1).ExecuteScalar<int>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp["title"].ToString() == "(nevermore, octofront)"));

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetween()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";

            var transaction = Substitute.For<IRelationalTransaction>();
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

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] BETWEEN @startvalue AND @endvalue)";

            var transaction = Substitute.For<IRelationalTransaction>();
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

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqual()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";

            var transaction = Substitute.For<IRelationalTransaction>();
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

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForWhereBetweenOrEqualExtension()
        {
            const string expectedSql = "SELECT COUNT(*) FROM dbo.[Todos]  WHERE ([Completed] >= @startvalue AND [Completed] <= @endvalue)";

            var transaction = Substitute.For<IRelationalTransaction>();
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

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForOrderBy()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem]  ORDER BY [Title]";
            var todoItem = new TodoItem {Id = 1, Title = "Complete Nevermore", Completed = false};
            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(new[] {todoItem});

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .OrderBy("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp.Count == 0));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(todoItem));
        }

        [Test]
        public void ShouldGetCorrectSqlQueryForOrderByDescending()
        {
            const string expectedSql = "SELECT TOP 1 * FROM dbo.[TodoItem]  ORDER BY [Title] DESC";
            var todoItem = new TodoItem { Id = 1, Title = "Complete Nevermore", Completed = false };
            var transaction = Substitute.For<IRelationalTransaction>();
            transaction.ExecuteReader<TodoItem>(Arg.Is<string>(s => s.Equals(expectedSql)), Arg.Any<CommandParameters>())
                .Returns(new[] { todoItem });

            var result = new QueryBuilder<TodoItem>(transaction, "TodoItem")
                .OrderByDescending("Title")
                .First();

            transaction.Received(1).ExecuteReader<TodoItem>(
                Arg.Is(expectedSql),
                Arg.Is<CommandParameters>(cp => cp.Count == 0));

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(todoItem));
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
}
