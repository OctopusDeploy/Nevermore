using System;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Advanced.Serialization;
using Nevermore.Mapping;
using Nevermore.Querying;
using Nevermore.Util;
using Newtonsoft.Json;
using NSubstitute;
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
            mappings.Resolve<object>().Returns(c => new EmptyMap());
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
                new DataModificationQueryBuilder(mappings, new NewtonsoftDocumentSerializer(mappings), s => null),
                queryExecutor
            );
        }

        [Test]
        public void ShouldGenerateDelete()
        {
            CreateQueryBuilder<object>()
                .Where("[Price] > 5")
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > 5)");
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedUnaryWhereClause()
        {
            CreateQueryBuilder<object>()
                .WhereParameterised("Price", UnarySqlOperand.GreaterThan, new Parameter("price"))
                .ParameterValue(5)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price)");
            parameters["price"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedBinaryWhereClause()
        {
            CreateQueryBuilder<object>()
                .WhereParameterised("Price", BinarySqlOperand.Between, new Parameter("LowerPrice"), new Parameter("UpperPrice"))
                .ParameterValues(5, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] BETWEEN @lowerprice AND @upperprice)");
            parameters["lowerprice"].Should().Be(5);
            parameters["upperprice"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithParameterisedArrayWhereClause()
        {
            CreateQueryBuilder<object>()
                .WhereParameterised("Price", ArraySqlOperand.In, new [] { new Parameter("LowerPrice"), new Parameter("UpperPrice") })
                .ParameterValues(new object[] {5, 10})
                .Delete();

            query.Should().Contain(@"WHERE ([Price] IN (@lowerprice, @upperprice))");
            parameters["lowerprice"].Should().Be(5);
            parameters["upperprice"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithUnaryWhereClause()
        {
            CreateQueryBuilder<object>()
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price)");
            parameters["price"].Should().Be(5);
        }

        [Test]
        public void ShouldGenerateDeleteWithBinaryWhereClause()
        {
            CreateQueryBuilder<object>()
                .Where("Price", BinarySqlOperand.Between, 5, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] BETWEEN @startvalue AND @endvalue)");
            parameters["startvalue"].Should().Be(5);
            parameters["endvalue"].Should().Be(10);
        }

        [Test]
        public void ShouldGenerateDeleteWithArrayWhereClause()
        {
            CreateQueryBuilder<object>()
                .Where("Price", ArraySqlOperand.In, new [] { 5, 10, 15 })
                .Delete();

            query.Should().Contain(@"WHERE ([Price] IN (@price0, @price1, @price2))");
            parameters["price0"].Should().Be("5");
            parameters["price1"].Should().Be("10");
            parameters["price2"].Should().Be("15");
        }

        [Test]
        public void ShouldGenerateUniqueParameterNames()
        {
            CreateQueryBuilder<object>()
                .Where("Price", UnarySqlOperand.GreaterThan, 5)
                .Where("Price", UnarySqlOperand.LessThan, 10)
                .Delete();

            query.Should().Contain(@"WHERE ([Price] > @price)
AND ([Price] < @price_1)");
            parameters["price"].Should().Be(5);
            parameters["price_1"].Should().Be(10);
        }

        [Test]
        public void ShouldThrowIfDifferentNumberOfParameterValuesProvided()
        {
            CreateQueryBuilder<object>()
                .WhereParameterised("Name", ArraySqlOperand.In, new[] {new Parameter("foo"), new Parameter("bar")})
                .Invoking(qb => qb.ParameterValues(new [] { "Foo" })).ShouldThrow<ArgumentException>();
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhere()
        {
#pragma warning disable NV0006
            CreateQueryBuilder<object>()
                .Where("fOo = @myVAriabLe AND Baz = @OthervaR")
                .Parameter("MyVariable", "Bar")
                .Parameter("OTHERVAR", "Bar")
                .Delete();
#pragma warning restore NV0006
            
            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereSingleParam()
        {
#pragma warning disable NV0006
            CreateQueryBuilder<object>()
                .Where("fOo", UnarySqlOperand.GreaterThan, "Bar")
                .Delete();
#pragma warning restore NV0006

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
#pragma warning disable NV0006
            CreateQueryBuilder<object>()
                .Where("fOo", BinarySqlOperand.Between, 1, 2)
                .Delete();
#pragma warning restore NV0006

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
#pragma warning disable NV0006
            CreateQueryBuilder<object>()
                .Where("fOo", UnarySqlOperand.Like, new[] { 1, 2, 3 })
                .Delete();
#pragma warning restore NV0006

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereIn()
        {
#pragma warning disable NV0006
            CreateQueryBuilder<object>()
                .Where("fOo", ArraySqlOperand.In, new[] { "BaR", "BaZ" })
                .Delete();
#pragma warning restore NV0006

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }
    }

    internal class EmptyMap : DocumentMap
    {
    }
}