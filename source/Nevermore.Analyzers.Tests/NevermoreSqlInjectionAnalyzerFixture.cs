using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.PlatformAbstractions;
using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
	public class NevermoreSqlInjectionAnalyzerFixture : NevermoreFixture
    {
        [Test]
        public void ShouldDetectSqlInjectionInInterpolatedStream()
        {
	        var code = @"
				var name = 'Robert';
				var args = new CommandParameterValues(
                    new
                    {
                        name,
                        age = 71
                    });
				transaction.Stream<Customer>($'select * from dbo.Customer where Name = {name}', args).ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertError(results, "This expression uses string concatenation");
        }

        [Test]
        public void ShouldDetectSqlInjectionInInterpolatedWhere()
        {
	        var code = @"
				var name = 'Robert';
				transaction.Query<Customer>().Where($'Name = {name}').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertError(results, "This expression uses string concatenation");
        }

        [Test]
        public void ShouldDetectSqlInjectionInConcatenatedWhere()
        {
	        var code = @"
				var name = 'Robert';
				transaction.Query<Customer>().Where('Name = ' + name).ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertError(results, "This expression uses string concatenation");
        }

        [Test]
        public void ShouldCompileIfPragmaIgnore()
        {
	        var code = @"
				#pragma warning disable NV0007
				// This call is safe from SQL injection because...
				var name = 'Robert';
				transaction.Query<Customer>().Where('Name = ' + name).ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }

        [Test]
        public void ShouldCompileIfInterpolatingANameOf()
        {
	        var code = @"
				transaction.Query<Customer>().Where($'Name = {nameof(System)}').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }

        [TestCase("1")]
        [TestCase("(int?) 1")]
        [TestCase("100000000L")]
        [TestCase("4.5")]
        [TestCase("4.5m")]
        [TestCase("true")]
        [TestCase("Environment.SpecialFolder.Cookies")]
        public void ShouldCompileIfAInterpolatingAPrimitive(string value)
        {
	        var code = $@"
				var name = {value};
				transaction.Query<Customer>().Where($'Name = ${{name}}').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }

        [Test]
        public void ShouldCompileIfInterpolatingAConstant()
        {
	        var code = @"
				const string name = 'Robert';
				transaction.Query<Customer>().Where($'Name = {name}').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }

        [Test]
        public void ShouldCompileIfInterpolatingAConstantFromAnotherType()
        {
	        var code = @"
				transaction.Query<Customer>().Where($'Name = {Customer.Constant}').ToList();
			";
	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }

        [Test]
        public void ShouldCompileIfInterpolatingAConstantFromANestedClass()
        {
	        var code = @"
				transaction.Query<Customer>().Where($'Name = {Customer.Attributes.Constant}').ToList();
			";
	        var results = CodeCompiler.Compile<NevermoreSqlInjectionAnalyzer>(code);
	        AssertPassed(results);
        }
    }
}