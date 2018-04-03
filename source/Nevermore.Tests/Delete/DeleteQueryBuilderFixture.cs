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
            return new DeleteQueryBuilder<TDocument>(transaction, tableName, Enumerable.Empty<IWhereClause>(), new CommandParameterValues());
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

            var parameter = new Parameter("price");
            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", UnarySqlOperand.GreaterThan, parameter)
                .Parameter(parameter, 5)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > @price)");
            values[parameter.ParameterName].Should().Be(5);
        }

        [Fact]
        public void ShouldGenerateDeleteWithParameterisedBinaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            var lowerPriceParameter = new Parameter("LowerPrice");
            var upperPriceParameter = new Parameter("UpperPrice");
            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", BinarySqlOperand.Between, lowerPriceParameter, upperPriceParameter)
                .Parameter(lowerPriceParameter, 5)
                .Parameter(upperPriceParameter, 10)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] BETWEEN @lowerprice AND @upperprice)");
            values[lowerPriceParameter.ParameterName].Should().Be(5);
            values[upperPriceParameter.ParameterName].Should().Be(10);
        }

        [Fact]
        public void ShouldGenerateDeleteWithParameterisedArrayWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            var lowerPriceParameter = new Parameter("LowerPrice");
            var upperPriceParameter = new Parameter("UpperPrice");
            CreateQueryBuilder<IDocument>("Orders")
                .WhereParameterised("Price", ArraySqlOperand.In, new [] { lowerPriceParameter, upperPriceParameter })
                .Parameter(lowerPriceParameter, 5)
                .Parameter(upperPriceParameter, 10)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] IN (@lowerprice, @upperprice))");
            values[lowerPriceParameter.ParameterName].Should().Be(5);
            values[upperPriceParameter.ParameterName].Should().Be(10);
        }

        [Fact]
        public void ShouldGenerateDeleteWithUnaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            transaction.ExecuteRawDeleteQuery(Arg.Do<string>(s => actual = s), Arg.Do<CommandParameterValues>(v => values = v));

            var parameter = new Parameter("price");
            CreateQueryBuilder<IDocument>("Orders")
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Delete();

            actual.Should().Be(@"DELETE FROM dbo.[Orders]
WHERE ([Price] > @price)");
            values[parameter.ParameterName].Should().Be(5);
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
WHERE ([Price] BETWEEN @startvalue AND @endvalue)");
            values["startvalue"].Should().Be(5);
            values["endvalue"].Should().Be(10);
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
WHERE ([Price] IN (@price0, @price1, @price2))");
            values["price0"].Should().Be("5");
            values["price1"].Should().Be("10");
            values["price2"].Should().Be("15");
        }
    }
}