using System.Linq;
using FluentAssertions;
using Nevermore.AST;
using Nevermore.Contracts;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class DeleteQueryVariableCasingFixture
    {
        private IRelationalTransaction transaction;
        private string query = null;
        private CommandParameterValues parameters = null;

        public DeleteQueryVariableCasingFixture()
        {
            query = null;
            parameters = null;
            transaction = Substitute.For<IRelationalTransaction>();
            transaction.WhenForAnyArgs(c => c.ExecuteRawDeleteQuery("", Arg.Any<CommandParameterValues>()))
                .Do(c =>
                {
                    query = c.Arg<string>();
                    parameters = c.Arg<CommandParameterValues>();
                });
        }

        IDeleteQueryBuilder<IId> CreateQueryBuilder()
        {
            return new DeleteQueryBuilder<IId>(transaction, new ParameterNameGenerator(), "Order", Enumerable.Empty<IWhereClause>(), new CommandParameterValues());
        }

        [Fact]
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

        [Fact]
        public void VariablesCasingIsNormalisedForWhereSingleParam()
        {
            CreateQueryBuilder()
                .Where("fOo", UnarySqlOperand.GreaterThan, "Bar")
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
            CreateQueryBuilder()
                .Where("fOo", BinarySqlOperand.Between, 1, 2)
                .Delete();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
            CreateQueryBuilder()
                .Where("fOo", UnarySqlOperand.Like, new[] { 1, 2, 3 })
                .Delete();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Fact]
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