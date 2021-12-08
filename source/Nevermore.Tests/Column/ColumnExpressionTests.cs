using System;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Advanced.QueryBuilders;
using NSubstitute;
using NUnit.Framework;

namespace Nevermore.Tests.Column
{
    public class ColumnExpressionTests
    {
        [Test]
        public void ColumnExpression()
        {
            var actual = CreateQueryBuilder()
                .Column(c => c.Foo)
                .Column(c => c.Bar)
                .Column(c => c.Baz)
                .DebugViewRawQuery();

            actual.ShouldBeEquivalentTo(@"SELECT [Foo],
[Bar],
[Baz]
FROM [dbo].[Records]
ORDER BY [Id]");
        }

        [Test]
        public void ColumnExpressionWithColumnAlias()
        {
            var actual = CreateQueryBuilder()
                .Column(c => c.Foo, "A")
                .Column(c => c.Bar, "B")
                .Column(c => c.Baz, "C")
                .DebugViewRawQuery();

            actual.ShouldBeEquivalentTo(@"SELECT [Foo] AS [A],
[Bar] AS [B],
[Baz] AS [C]
FROM [dbo].[Records]
ORDER BY [Id]");
        }

        [Test]
        public void ColumnExpressionWithColumnAliasAndTableAlias()
        {
            const string tableAlias = "MyTable";
            var actual = CreateQueryBuilder()
                .Alias(tableAlias)
                .Column(c => c.Foo, "A", tableAlias)
                .Column(c => c.Bar, "B", tableAlias)
                .Column(c => c.Baz, "C", tableAlias)
                .DebugViewRawQuery();

            actual.ShouldBeEquivalentTo(@"SELECT MyTable.[Foo] AS [A],
MyTable.[Bar] AS [B],
MyTable.[Baz] AS [C]
FROM [dbo].[Records] MyTable
ORDER BY [Id]");
        }

        static ITableSourceQueryBuilder<Record> CreateQueryBuilder()
        {
            var memberInfos = Activator.CreateInstance<Record>().GetType().GetProperties();
            var columnNames = memberInfos.Select(x => x.Name).ToList();
            
            return new TableSourceQueryBuilder<Record>("Records", 
                "dbo",
                "Id",
                Substitute.For<IRelationalTransaction>(), 
                new TableAliasGenerator(), 
                new UniqueParameterNameGenerator(), 
                new CommandParameterValues(),
                new Parameters(),
                new ParameterDefaults()
            );
        }

        class Record
        {
            public string Foo { get; }
            public int Bar { get; }
            public DateTime Baz { get; }
            public string Qux { get; }
        }
    }
}