using System.Linq;
using FluentAssertions;
using Nevermore.Contracts;
using NSubstitute;
using Xunit;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class VariableCasingFixture
    {
        private IRelationalTransaction transaction;
        private string query = null;
        private CommandParameterValues parameters = null;

        public VariableCasingFixture()
        {
            query = null;
            parameters = null;
            transaction = Substitute.For<IRelationalTransaction>();
            transaction.WhenForAnyArgs(c => c.ExecuteReader<IId>("", Arg.Any<CommandParameterValues>()))
                .Do(c =>
                {
                    query = c.Arg<string>();
                    parameters = c.Arg<CommandParameterValues>();
                });
        }

        IQueryBuilder<IId> CreateQueryBuilder()
        {
            return new TableSourceQueryBuilder<IId>("Order", transaction, new TableAliasGenerator(), new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhere()
        {
            CreateQueryBuilder()
                .Where("fOo = @myVAriabLe AND Baz = @OthervaR")
                .Parameter("MyVariable", "Bar")
                .Parameter("OTHERVAR", "Bar")
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereSingleParam()
        {
            CreateQueryBuilder()
                .Where("fOo", SqlOperand.GreaterThan, "Bar")
                .ToList();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
            CreateQueryBuilder()
                .Where("fOo", SqlOperand.Between, 1, 2)
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
            CreateQueryBuilder()
                .Where("fOo", SqlOperand.Contains, new[] { 1, 2, 3 })
                .ToList();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Fact]
        public void VariablesCasingIsNormalisedForWhereIn()
        {
            CreateQueryBuilder()
                .Where("fOo", SqlOperand.In, new[] { "BaR", "BaZ" })
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }
    }
}