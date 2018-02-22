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
        }
        
        protected static (IQueryBuilder<Foo> builder, (Parameters parameters, CommandParameterValues paramValues)) NewQueryBuilder()
        {
            var parameters = new Parameters();
            var captures = new CommandParameterValues();
            var builder = new QueryBuilder<Foo, TableSelectBuilder>(
                new TableSelectBuilder(new SimpleTableSource("Foo")),
                Substitute.For<IRelationalTransaction>(),
                new TableAliasGenerator(),
                captures,
                parameters
            );

            return (builder, (parameters, captures));
        }
    }
}