using System;
using Nevermore.AST;
using NSubstitute;

namespace Nevermore.Tests.Query
{
    public class LinqTestBase
    {
        protected enum Bar
        {
            A = 2,
            B
        }

        protected class Foo
        {
            public int Int { get; set; }
            public string String { get; set; }
            public Bar Enum { get; set; }
            public DateTime DateTime { get; set; }
            public bool Bool { get; set; }
        }
        
        protected static (IQueryBuilder<Foo> builder, (Parameters parameters, CommandParameterValues paramValues)) NewQueryBuilder(IUniqueParameterNameGenerator uniqueParameterNameGenerator = null)
        {
            var parameters = new Parameters();
            var captures = new CommandParameterValues();
            var builder = new QueryBuilder<Foo, TableSelectBuilder>(
                new TableSelectBuilder(new SimpleTableSource("Foo")),
                Substitute.For<IRelationalTransaction>(),
                new TableAliasGenerator(),
                uniqueParameterNameGenerator ?? CreateSubstituteParameterNameGenerator(), 
                captures,
                parameters,
                new ParameterDefaults(),
                new RelationalStoreConfiguration(null)
            );

            return (builder, (parameters, captures));
        }

        static IUniqueParameterNameGenerator CreateSubstituteParameterNameGenerator()
        {
            var parameterNameGenerator = Substitute.For<IUniqueParameterNameGenerator>();
            parameterNameGenerator.GenerateUniqueParameterName(Arg.Any<string>()).Returns(c => c.Arg<string>());
            return parameterNameGenerator;
        }
    }
}