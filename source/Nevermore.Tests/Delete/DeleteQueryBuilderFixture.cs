using System;
using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests.Delete
{
    public class DeleteQueryBuilderFixture
    {
        readonly IRelationalTransaction transaction;

        public DeleteQueryBuilderFixture()
        {
            transaction = Substitute.For<IRelationalTransaction>();
        }

        IDeleteQueryBuilder<TDocument> CreateQueryBuilder<TDocument>(Action<Type, Where, CommandParameterValues, int?> executeDelete) where TDocument : class
        {
            return new DeleteQueryBuilder<TDocument>(new UniqueParameterNameGenerator(), executeDelete, Enumerable.Empty<IWhereClause>(), new CommandParameterValues());
        }

        [Test]
        public void ShouldGenerateDelete()
        {
            string actual = null;

            void ExecuteDelete(Type _, Where where, CommandParameterValues __, int? ___) => actual = where.GenerateSql().Trim();

            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .Where("[Price] > 5")
                .Delete();

            actual.Should().Be(@"WHERE ([Price] > 5)");
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedUnaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }
            
            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .WhereParameterised("Price", UnarySqlOperand.GreaterThan, new Parameter("price"))
                .ParameterValue(5)
                .Delete();

            actual.Should().Be(@"WHERE ([Price] > @price_0)");
            values["price_0"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedBinaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }

            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .WhereParameterised("Price", BinarySqlOperand.Between, new Parameter("LowerPrice"), new Parameter("UpperPrice"))
                .ParameterValues(5, 10)
                .Delete();

            actual.Should().Be(@"WHERE ([Price] BETWEEN @lowerprice_0 AND @upperprice_1)");
            values["lowerprice_0"].Should().Be(5);
            values["upperprice_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedArrayWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }

            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .WhereParameterised("Price", ArraySqlOperand.In, new [] { new Parameter("LowerPrice"), new Parameter("UpperPrice") })
                .ParameterValues(new object[] {5, 10})
                .Delete();

            actual.Should().Be(@"WHERE ([Price] IN (@lowerprice_0, @upperprice_1))");
            values["lowerprice_0"].Should().Be(5);
            values["upperprice_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithUnaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }

            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Delete();

            actual.Should().Be(@"WHERE ([Price] > @price_0)");
            values["price_0"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithBinaryWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }

            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .Where("Price", BinarySqlOperand.Between, 5, 10)
                .Delete();

            actual.Should().Be(@"WHERE ([Price] BETWEEN @startvalue_0 AND @endvalue_1)");
            values["startvalue_0"].Should().Be(5);
            values["endvalue_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithArrayWhereClause()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }
            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .Where("Price", ArraySqlOperand.In, new [] { 5, 10, 15 })
                .Delete();

            actual.Should().Be(@"WHERE ([Price] IN (@price0_0, @price1_1, @price2_2))");
            values["price0_0"].Should().Be("5");
            values["price1_1"].Should().Be("10");
            values["price2_2"].Should().Be("15");
        }

        [Test]
        public void ShouldGenerateUniqueParameterNames()
        {
            string actual = null;
            CommandParameterValues values = null;
            void ExecuteDelete(Type _, Where where, CommandParameterValues cmdValue, int? ___)
            {
                actual = @where.GenerateSql().Trim();
                values = cmdValue;
            }
            CreateQueryBuilder<IDocument>(ExecuteDelete)
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Where("Price", UnarySqlOperand.LessThan, 10)
                .Delete();

            actual.Should().Be(@"WHERE ([Price] > @price_0)
AND ([Price] < @price_1)");
            values["price_0"].Should().Be(5);
            values["price_1"].Should().Be(10);
        }

        [Test]
        public void ShouldThrowIfDifferentNumberOfParameterValuesProvided()
        {
            CreateQueryBuilder<IDocument>((t, w, p, _) => { })
                .WhereParameterised("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterValues(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }
    }
}