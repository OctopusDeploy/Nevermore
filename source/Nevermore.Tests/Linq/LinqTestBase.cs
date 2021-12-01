using System;
using System.Linq;
using Nevermore.Advanced;
using Nevermore.Querying.AST;
using NSubstitute;

namespace Nevermore.Tests.Linq
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
            
            var memberInfos = Activator.CreateInstance<Foo>().GetType().GetProperties();
            var columnNames = memberInfos.Select(x => x.Name).ToArray();
            
            var builder = new QueryBuilder<Foo, TableSelectBuilder>(
                new TableSelectBuilder(new SimpleTableSource("Foo", "dbo", columnNames), new Querying.AST.Column("Id")),
                Substitute.For<IRelationalTransaction>(),
                new TableAliasGenerator(),
                uniqueParameterNameGenerator ?? CreateSubstituteParameterNameGenerator(), 
                captures,
                parameters,
                new ParameterDefaults()
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