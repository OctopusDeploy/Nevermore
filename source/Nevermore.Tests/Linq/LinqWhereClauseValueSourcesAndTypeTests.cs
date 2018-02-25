using System;
using System.Linq;
using FluentAssertions;
using Nevermore.AST;
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
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == "Bar");

            AssertResult(result, captures);
        }


        [Fact]
        public void WithLocalVariable()
        {
            var (builder, captures) = NewQueryBuilder();

            var bar = "Bar";

            var result = builder.Where(f => f.String == bar);

            AssertResult(result, captures);
        }

        [Fact]
        public void WithField()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == barField);

            AssertResult(result, captures);
        }

        [Fact]
        public void WithProperty()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarProperty);

            AssertResult(result, captures);
        }

        [Fact]
        public void WithMethod()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarMethod());

            AssertResult(result, captures);
        }

        [Fact]
        public void WithPropertyFromObject()
        {
            var (builder, captures) = NewQueryBuilder();

            var obj = new Foo {String = "Bar"};

            var result = builder.Where(f => f.String == obj.String);

            AssertResult(result, captures);
        }
        
        [Fact]
        public void WithInt()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Int == 2);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Int] = @int) ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("int");
            paramValues.Should().Contain("int", 2);
        }
        
        [Fact]
        public void WithDateTime()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.DateTime == new DateTime(2011,1,1,4,5,6));

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([DateTime] = @datetime) ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("datetime");
            paramValues.Should().Contain("datetime", new DateTime(2011,1,1,4,5,6));
        }
        
        [Fact]
        public void WithEnum()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Enum == Bar.A);

            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([Enum] = @enum) ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("enum");
            paramValues.Should().Contain("enum", Bar.A);
        }
        
    
        static void AssertResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures)
        {
            var (parameters, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be("SELECT * FROM dbo.[Foo] WHERE ([String] = @string) ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("string");
            paramValues.Should().Contain("string", "Bar");
        }
    }
}