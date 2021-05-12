using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
	public class NevermoreWhereExpressionAnalyzerFixture : NevermoreFixture
    {

	    [Test]
	    public void ShouldPassForStringEquality()
	    {
		    var code = @"
				transaction.Query<Customer>().Where(c => c.FirstName == '').ToList();
			";

		    var results = CodeCompiler.Compile<NevermoreWhereExpressionAnalyzer>(code);
		    AssertPassed(results);
	    }

	    [Test]
	    public void ShouldPassForStartsWith()
	    {
		    var code = @"
				transaction.Query<Customer>().Where(c => c.FirstName.StartsWith('Rob')).ToList();
			";

		    var results = CodeCompiler.Compile<NevermoreWhereExpressionAnalyzer>(code);
		    AssertPassed(results);
	    }

	    [Test]
	    public void ShouldPassForNullForgivingOperator()
	    {
		    var code = @"
				transaction.Query<Customer>().Where(c => c.FirstName!.StartsWith('Rob')).ToList();
			";

		    var results = CodeCompiler.Compile<NevermoreWhereExpressionAnalyzer>(code);
		    AssertPassed(results);
	    }

        [Test]
        public void ShouldFailForPropertyAccess()
        {
	        var code = @"
				transaction.Query<Customer>().Where(c => c.FirstName.Substring(1) == 'ob').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreWhereExpressionAnalyzer>(code);
	        AssertError(results, "Nevermore LINQ support will not be able to translate this expression: Cannot translate call to method 'Substring'. Nevermore can only translate: \"In\", \"NotIn\", \"Contains\", \"StartsWith\", \"EndsWith\"");
        }
    }
}