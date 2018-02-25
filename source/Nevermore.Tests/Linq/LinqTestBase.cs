using System;
using Nevermore.Joins;
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
        
        protected static IQueryBuilder<Foo> NewQueryBuilder()
        {
            var builder = new QueryBuilder<Foo>(
                Substitute.For<IRelationalTransaction>(),
                "Foo"                
            );

            return builder;
        }
    }
}