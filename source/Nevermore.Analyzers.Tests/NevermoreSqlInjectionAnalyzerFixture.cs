using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
	public class NevermoreSqlInjectionAnalyzerFixture
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

        void AssertError(List<Diagnostic> results, string error)
        {
	        results.Count.Should().Be(1);
	        results[0].GetMessage().Should().Contain(error);
        }

        void AssertPassed(List<Diagnostic> results)
        {
	        if (results.Count == 0)
		        return;

	        var errors = results.Select(r => r.GetMessage());
	        Assert.Fail(string.Join(Environment.NewLine, errors));
        }
    }
}