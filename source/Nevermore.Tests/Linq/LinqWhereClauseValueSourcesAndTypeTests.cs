using System;
using System.Linq;
using FluentAssertions;
using Nevermore.Tests.Query;
using Xunit;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseValueSourcesAndTypeTests : LinqTestBase
    {
        const string BarConst = "Bar";
        string barField = "Bar";
        string BarProperty { get; } = "Bar";
        string BarMethod() => "Bar";

        
        [Fact]
        public void WithConstant()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.String == "Bar");

            AssertResult(result);
        }


        [Fact]
        public void WithLocalVariable()
        {
            var builder = NewQueryBuilder();

            var bar = "Bar";

            var result = builder.Where(f => f.String == bar);

            AssertResult(result);
        }

        [Fact]
        public void WithField()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.String == barField);

            AssertResult(result);
        }

        [Fact]
        public void WithProperty()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarProperty);

            AssertResult(result);
        }

        [Fact]
        public void WithMethod()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarMethod());

            AssertResult(result);
        }

        [Fact]
        public void WithPropertyFromObject()
        {
            var builder = NewQueryBuilder();

            var obj = new Foo {String = "Bar"};

            var result = builder.Where(f => f.String == obj.String);

            AssertResult(result);
        }
        
        [Fact]
        public void WithInt()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Int == 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] = @int) ORDER BY [Id]");

            builder.QueryGenerator.QueryParameters.Single().Key.Should().Be("int");
            builder.QueryGenerator.QueryParameters.Should().Contain("int", 2);
        }
        
        [Fact]
        public void WithDateTime()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.DateTime == new DateTime(2011,1,1,4,5,6));

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([DateTime] = @datetime) ORDER BY [Id]");

            builder.QueryGenerator.QueryParameters.Single().Key.Should().Be("datetime");
            builder.QueryGenerator.QueryParameters.Should().Contain("datetime", new DateTime(2011,1,1,4,5,6));
        }
        
        [Fact]
        public void WithEnum()
        {
            var builder = NewQueryBuilder();

            var result = builder.Where(f => f.Enum == Bar.A);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Enum] = @enum) ORDER BY [Id]");

            builder.QueryGenerator.QueryParameters.Single().Key.Should().Be("enum");
            builder.QueryGenerator.QueryParameters.Should().Contain("enum", Bar.A);
        }
        
    
        static void AssertResult(IQueryBuilder<Foo> result)
        {
            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([String] = @string) ORDER BY [Id]");

            result.QueryGenerator.QueryParameters.Single().Key.Should().Be("string");
            result.QueryGenerator.QueryParameters.Should().Contain("string", "Bar");
        }
    }
}