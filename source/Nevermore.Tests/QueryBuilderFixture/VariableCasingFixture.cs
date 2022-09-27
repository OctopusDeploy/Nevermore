using System;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Advanced.QueryBuilders;
using NSubstitute;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

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
            transaction.WhenForAnyArgs(c => c.Stream<object>("", Arg.Any<CommandParameterValues>()))
                .Do(c =>
                {
                    query = c.Arg<string>();
                    parameters = c.Arg<CommandParameterValues>();
                });
        }

        IQueryBuilder<object> CreateQueryBuilder()
        {
            var memberInfos = Activator.CreateInstance<object>().GetType().GetProperties();
            var columnNames = memberInfos.Select(x => x.Name).ToList();
            
            return new TableSourceQueryBuilder<object>(
            "Order",
            "dbo",
            "Id",
            null,
            null,
            transaction,
            new TableAliasGenerator(),
            new UniqueParameterNameGenerator(),
            new CommandParameterValues(),
            new Parameters(),
            new ParameterDefaults());
        }

        [Test]
        public void VariablesCasingIsNormalisedForWhere()
        {
            #pragma warning disable NV0006
            CreateQueryBuilder()
                .Where("fOo = @myVAriabLe AND Baz = @OthervaR")
                .Parameter("MyVariable", "Bar")
                .Parameter("OTHERVAR", "Bar")
                .ToList();
            
            #pragma warning restore NV0006

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForUnaryWhere()
        {
            CreateQueryBuilder()
                .Where("fOo", UnarySqlOperand.GreaterThan, "Bar")
                .ToList();

            parameters.Count.Should().Be(1);
            var parameter = "@" + parameters.Keys.Single();
            query.Should().Contain(parameter, "Should contain " + parameter);
        }

        [Test]
        public void VariablesCasingIsNormalisedForBinaryWhere()
        {
            CreateQueryBuilder()
                .Where("fOo", BinarySqlOperand.Between, 1, 2)
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }

        [Test]
        public void VariablesCasingIsNormalisedForArrayWhere()
        {
            CreateQueryBuilder()
                .Where("fOo", ArraySqlOperand.In, new[] { "BaR", "BaZ" })
                .ToList();

            parameters.Count.Should().Be(2);
            foreach (var parameter in parameters)
                query.Should().Contain("@" + parameter.Key, "Should contain @" + parameter.Key);
        }
    }
}