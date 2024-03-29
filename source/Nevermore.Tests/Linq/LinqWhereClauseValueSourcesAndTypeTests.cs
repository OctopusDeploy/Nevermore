﻿using System;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests.Linq
{
    public class LinqWhereClauseValueSourcesAndTypeTests : LinqTestBase
    {
        const string BarConst = "Bar";
        string barField = "Bar";
        string BarProperty { get; } = "Bar";
        string BarMethod() => "Bar";

        
        [Test]
        public void WithConstant()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == "Bar");

            AssertResult(result, captures);
        }

        [Test]
        public void WithLocalVariable()
        {
            var (builder, captures) = NewQueryBuilder();

            var bar = "Bar";

            var result = builder.Where(f => f.String == bar);

            AssertResult(result, captures);
        }

        [Test]
        public void WithField()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == barField);

            AssertResult(result, captures);
        }

        [Test]
        public void WithProperty()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarProperty);

            AssertResult(result, captures);
        }

        [Test]
        public void WithMethod()
        {
            var (builder, captures) = NewQueryBuilder();

            var result = builder.Where(f => f.String == BarMethod());

            AssertResult(result, captures);
        }

        [Test]
        public void WithPropertyFromObject()
        {
            var (builder, captures) = NewQueryBuilder();

            var obj = new Foo {String = "Bar"};

            var result = builder.Where(f => f.String == obj.String);

            AssertResult(result, captures);
        }
        
        [Test]
        public void WithInt()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Int == 2);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Int] = @int)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("int");
            paramValues.Should().Contain("int", 2);
        }
        
        [Test]
        public void WithBoolean()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Bool);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Bool] = @bool)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("bool");
            paramValues.Should().Contain("bool", true);
        }
        
        [Test]
        public void WithNotBoolean()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => !f.Bool);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Bool] = @bool)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("bool");
            paramValues.Should().Contain("bool", false);
        }
        
        [Test]
        public void WithBooleanComparison()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Bool == false);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Bool] = @bool)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("bool");
            paramValues.Should().Contain("bool", false);
        }
        
        [Test]
        public void WithDateTime()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.DateTime == new DateTime(2011,1,1,4,5,6));

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([DateTime] = @datetime)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("datetime");
            paramValues.Should().Contain("datetime", new DateTime(2011,1,1,4,5,6));
        }
        
        [Test]
        public void WithEnum()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var result = builder.Where(f => f.Enum == Bar.A);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Enum] = @enum)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("enum");
            paramValues.Should().Contain("enum", Bar.A);
        }
        
        [Test]
        public void WithEnumFromAProperty()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var input = new
            {
                Enum = Bar.A
            };
            
            var result = builder.Where(f => f.Enum == input.Enum);

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Enum] = @enum)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("enum");
            paramValues.Should().Contain("enum", Bar.A);
        }
        
        [Test]
        public void WithEnumContainsIn()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var input = new[] {Bar.A, Bar.B};
            
            var result = builder.Where(f => input.Contains(f.Enum));

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Enum] IN (@enum1, @enum2))
ORDER BY [Id]");

            paramValues.Should().Contain("enum1", Bar.A);
            paramValues.Should().Contain("enum2", Bar.B);
        }
        
        [Test]
        public void WithEnumIn()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var input = new[] {Bar.A, Bar.B};
            
            var result = builder.Where(f => f.Enum.In(input));

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Enum] IN (@enum1, @enum2))
ORDER BY [Id]");

            paramValues.Should().Contain("enum1", Bar.A);
            paramValues.Should().Contain("enum2", Bar.B);
        }
        
        [Test]
        public void WithEnumNotIn()
        {
            var (builder, (parameters, paramValues)) = NewQueryBuilder();

            var input = new[] {Bar.A, Bar.B};
            
            var result = builder.Where(f => f.Enum.NotIn(input));

            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([Enum] NOT IN (@enum1, @enum2))
ORDER BY [Id]");

            paramValues.Should().Contain("enum1", Bar.A);
            paramValues.Should().Contain("enum2", Bar.B);
        }
    
        static void AssertResult(IQueryBuilder<Foo> result, (Parameters, CommandParameterValues) captures)
        {
            var (parameters, paramValues) = captures;
            result.DebugViewRawQuery()
                .Should()
                .Be(@"SELECT Int,String,Enum,DateTime,Bool
FROM [dbo].[Foo]
WHERE ([String] = @string)
ORDER BY [Id]");

            parameters.Single().ParameterName.Should().Be("string");
            paramValues.Should().Contain("string", "Bar");
        }
    }
}