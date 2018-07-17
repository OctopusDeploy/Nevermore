using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class DeleteQueryVariableCasingFixture
    {
        private string query = null;
        private CommandParameterValues parameters = null;

        public DeleteQueryVariableCasingFixture()
        {
            query = null;
            parameters = null;
        }

        IDeleteQueryBuilder<IId> CreateQueryBuilder()
        {
            return new DeleteQueryBuilder<IId>(
                new UniqueParameterNameGenerator(),
                (_, q, p, __) =>
                {
                    query = q.GenerateSql();
                    parameters = p;
                }, 
                Enumerable.Empty<IWhereClause>(), 
                new CommandParameterValues()
                );
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhere()
        {
            CreateQueryBuilder()
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
            CreateQueryBuilder()
                .Where("fOo", UnarySqlOperand.GreaterThan, "Bar")
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
            CreateQueryBuilder()
                .Where("fOo", BinarySqlOperand.Between, 1, 2)
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
            CreateQueryBuilder()
                .Where("fOo", UnarySqlOperand.Like, new[] { 1, 2, 3 })
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereIn()
        {
            CreateQueryBuilder()
                .Where("fOo", ArraySqlOperand.In, new[] { "BaR", "BaZ" })
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }
    }
}