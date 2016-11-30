using System.Linq;
using FluentAssertions;
using Nevermore.Contracts;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests.QueryBuilderFixture
{
    public class VariableCasingFixture
    {
        private IRelationalTransaction transaction;
        private string query = null;
        private CommandParameters parameters = null;
        [SetUp]
        public void SetUp()
        {
            query = null;
            parameters = null;
            transaction = Substitute.For<IRelationalTransaction>();
            transaction.WhenForAnyArgs(c => c.ExecuteReader<IId>("", Arg.Any<CommandParameters>()))
                .Do(c =>
                {
                    query = c.Arg<string>();
                    parameters = c.Arg<CommandParameters>();
                });
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhere()
        {
            new QueryBuilder<IId>(transaction, "Order")
                .Where("fOo = @myVAriabLe AND Baz = @OthervaR")
                .Parameter("MyVariable", "Bar")
                .Parameter("OTHERVAR", "Bar")
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereSingleParam()
        {
            new QueryBuilder<IId>(transaction, "Order")
                .Where("fOo", SqlOperand.GreaterThan, "Bar")
                .ToList();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereTwoParam()
        {
            new QueryBuilder<IId>(transaction, "Order")
                .Where("fOo", SqlOperand.Between, 1, 2)
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhereParamArray()
        {
            new QueryBuilder<IId>(transaction, "Order")
                .Where("fOo", SqlOperand.Contains, new[] { 1, 2, 3 })
                .ToList();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }
    }
}