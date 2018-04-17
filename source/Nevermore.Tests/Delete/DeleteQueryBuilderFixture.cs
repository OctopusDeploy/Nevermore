using System;
using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.Delete
{
    public class DeleteQueryBuilderFixture
    {
        readonly IRelationalTransaction transaction;

        public DeleteQueryBuilderFixture()
        {
            transaction = Substitute.For<IRelationalTransaction>();
        }

        IDeleteQueryBuilder<TDocument> CreateQueryBuilder<TDocument>(string tableName) where TDocument : class
        {
            return new DeleteQueryBuilder<TDocument>(transaction, new UniqueParameterNameGenerator(), tableName, Enumerable.Empty<IWhereClause>(), new CommandParameterValues());
        }

        [Fact]
        public void ShouldGenerateDelete()
        {
            string actual = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Any<CommandParameterValues>());

            CreateQueryBuilder<IDocument>("Orders")
                .Where("[Price] > 5")
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > 5)");
        }

        [Fact]
        public void ShouldGenerateDeleteWithParameterisedUnaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", UnarySqlOperand.GreaterThan, new Parameter("price"))
                .ParameterValue(5)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > @price_0)");
            values["price_0"].Should().Be(5);
        }

        [Fact]
        public void ShouldGenerateDeleteWithParameterisedBinaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", BinarySqlOperand.Between, new Parameter("LowerPrice"), new Parameter("UpperPrice"))
                .ParameterValues(5, 10)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] BETWEEN @lowerprice_0 AND @upperprice_1)");
            values["lowerprice_0"].Should().Be(5);
            values["upperprice_1"].Should().Be(10);
        }

        [Fact]
        public void ShouldGenerateDeleteWithParameterisedArrayWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", ArraySqlOperand.In, new [] { new Parameter("LowerPrice"), new Parameter("UpperPrice") })
                .ParameterValues(new object[] {5, 10})
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] IN (@lowerprice_0, @upperprice_1))");
            values["lowerprice_0"].Should().Be(5);
            values["upperprice_1"].Should().Be(10);
        }

        [Fact]
        public void ShouldGenerateDeleteWithUnaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > @price_0)");
            values["price_0"].Should().Be(5);
        }

        [Fact]
        public void ShouldGenerateDeleteWithBinaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .Where("Price", BinarySqlOperand.Between, 5, 10)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] BETWEEN @startvalue_0 AND @endvalue_1)");
            values["startvalue_0"].Should().Be(5);
            values["endvalue_1"].Should().Be(10);
        }

        [Fact]
        public void ShouldGenerateDeleteWithArrayWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .Where("Price", ArraySqlOperand.In, new [] { 5, 10, 15 })
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] IN (@price0_0, @price1_1, @price2_2))");
            values["price0_0"].Should().Be("5");
            values["price1_1"].Should().Be("10");
            values["price2_2"].Should().Be("15");
        }

        [Fact]
        public void ShouldGenerateUniqueParameterNames()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            CreateQueryBuilder<IDocument>("Orders")
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Where("Price", UnarySqlOperand.LessThan, 10)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > @price_0)
AND ([Price] < @price_1)");
            values["price_0"].Should().Be(5);
            values["price_1"].Should().Be(10);
        }

        [Fact]
        public void ShouldThrowIfDifferentNumberOfParameterValuesProvided()
        {
            CreateQueryBuilder<IDocument>("Todo")
                .WhereParameterised("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterValues(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }
    }
}