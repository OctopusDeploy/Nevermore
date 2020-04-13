using System;
using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.Util;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;

namespace Nevermore.Tests.Delete
{
    public class DeleteQueryBuilderFixture
    {
        readonly IWriteQueryExecutor queryExecutor;
        string query;
        CommandParameterValues parameters;
        IDocumentMapRegistry mappings;

        public DeleteQueryBuilderFixture()
        {
            query = null;
            parameters = null;

            mappings = Substitute.For<IDocumentMapRegistry>();
            mappings.Resolve<IDocument>().Returns(c => new EmptyMap());
            mappings.Resolve(Arg.Any<Type>()).Returns(c => new EmptyMap());
            
            queryExecutor = Substitute.For<IWriteQueryExecutor>();
            queryExecutor.ExecuteNonQuery(Arg.Any<PreparedCommand>()).Returns(info =>
            {
                query = info.Arg<PreparedCommand>().Statement;
                parameters = info.Arg<PreparedCommand>().ParameterValues;
                return 1;
            });
        }

        IDeleteQueryBuilder<TDocument> CreateQueryBuilder<TDocument>() where TDocument : class
        {
            return new DeleteQueryBuilder<TDocument>(
                new UniqueParameterNameGenerator(),
                new DataModificationQueryBuilder(mappings, new JsonSerializerSettings(), s => null),
                queryExecutor
            );
        }

        [Test]
        public void ShouldGenerateDelete()
        {
            CreateQueryBuilder<IDocument>()
                .Where("[Price] > 5")
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > 5)");
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedUnaryWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .WhereParameterised("Price", UnarySqlOperand.GreaterThan, new Parameter("price"))
                .ParameterValue(5)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price_0)");
            parameters["price_0"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedBinaryWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .WhereParameterised("Price", BinarySqlOperand.Between, new Parameter("LowerPrice"), new Parameter("UpperPrice"))
                .ParameterValues(5, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] BETWEEN @lowerprice_0 AND @upperprice_1)");
            parameters["lowerprice_0"].Should().Be(5);
            parameters["upperprice_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedArrayWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .WhereParameterised("Price", ArraySqlOperand.In, new [] { new Parameter("LowerPrice"), new Parameter("UpperPrice") })
                .ParameterValues(new object[] {5, 10})
                .Delete();

            query.Should().Contain(@"WHERE ([Price] IN (@lowerprice_0, @upperprice_1))");
            parameters["lowerprice_0"].Should().Be(5);
            parameters["upperprice_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithUnaryWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price_0)");
            parameters["price_0"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithBinaryWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .Where("Price", BinarySqlOperand.Between, 5, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] BETWEEN @startvalue_0 AND @endvalue_1)");
            parameters["startvalue_0"].Should().Be(5);
            parameters["endvalue_1"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithArrayWhereClause()
        {
            CreateQueryBuilder<IDocument>()
                .Where("Price", ArraySqlOperand.In, new [] { 5, 10, 15 })
                .Delete();

            query.Should().Contain(@"WHERE ([Price] IN (@price0_0, @price1_1, @price2_2))");
            parameters["price0_0"].Should().Be("5");
            parameters["price1_1"].Should().Be("10");
            parameters["price2_2"].Should().Be("15");
        }

        [Test]
        public void ShouldGenerateUniqueParameterNames()
        {
            CreateQueryBuilder<IDocument>()
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Where("Price", UnarySqlOperand.LessThan, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price_0)
AND ([Price] < @price_1)");
            parameters["price_0"].Should().Be(5);
            parameters["price_1"].Should().Be(10);
        }

        [Test]
        public void ShouldThrowIfDifferentNumberOfParameterValuesProvided()
        {
            CreateQueryBuilder<IDocument>()
                .WhereParameterised("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterValues(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhere()
        {
            CreateQueryBuilder<IDocument>()
                .Where("fOo = @myVAriabLe AND Baz = @OthervaR")
                .Parameter("MyVariable", "Bar")
                .Parameter("OTHERVAR", "Bar")
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereSingleParam()
        {
            CreateQueryBuilder<IDocument>()
                .Where("fOo", UnarySqlOperand.GreaterThan, "Bar")
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
            CreateQueryBuilder<IDocument>()
                .Where("fOo", BinarySqlOperand.Between, 1, 2)
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
            CreateQueryBuilder<IDocument>()
                .Where("fOo", UnarySqlOperand.Like, new[] { 1, 2, 3 })
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereIn()
        {
            CreateQueryBuilder<IDocument>()
                .Where("fOo", ArraySqlOperand.In, new[] { "BaR", "BaZ" })
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }
    }

    internal class EmptyMap : DocumentMap
    {
    }
}