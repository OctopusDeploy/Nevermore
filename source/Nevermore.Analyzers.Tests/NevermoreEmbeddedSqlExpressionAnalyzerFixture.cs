using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
	public class NevermoreEmbeddedSqlExpressionAnalyzerFixture : NevermoreFixture
    {
        [Test]
        public void ShouldDetectMissingParameter()
        {
	        var code = @"
				transaction.Query<Customer>().Where('FirstName = @name').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreEmbeddedSqlExpressionAnalyzer>(code);
	        AssertError(results, "The query refers to the parameter '@name', but no value for the parameter is being passed to the query");
        }

        [Test]
        public void ShouldDetectMisspelledParameter()
        {
	        var code = @"
				transaction.Query<Customer>().Where('FirstName = @name').Parameter('nome', 'Robert').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreEmbeddedSqlExpressionAnalyzer>(code);
	        AssertError(results, "The query refers to the parameter '@name', but no value for the parameter is being passed to the query");
	        AssertError(results, "Detected the following parameters: @nome");
        }

        [Test]
        public void ShouldDetectIncorrectlyCasedParameter()
        {
	        var code = @"
				transaction.Query<Customer>().Where('FirstName = @name').Parameter('Name', 'Robert').ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreEmbeddedSqlExpressionAnalyzer>(code);

	        AssertError(results, "The query refers to the parameter '@name', but the parameter being passed uses different casing.");
        }

        [Test]
        public void ShouldDetectCommandParametersWithObjectSyntax()
        {
	        var code = @"
				var name = 'Robert';
				var args = new CommandParameterValues(
                    new
                    {
                        name,
                        age = 71
                    });
				transaction.Stream<Customer>('select * from dbo.Customer where Name = @name and Age = @age', args).ToList();
			";

	        var results = CodeCompiler.Compile<NevermoreEmbeddedSqlExpressionAnalyzer>(code);
	        AssertPassed(results);
        }

        [Test]
        public void ShouldHandlePropertiesSetOnCommandParameterValuesAsWellAsParameters()
        {
            var code = @"
                var parameters = new CommandParameterValues
                {
                    [""searchParam""] = ""abc"",
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                var results = transaction.Stream<string>(""FullTextSearch"", parameters).ToList();
            ";

            var results = CodeCompiler.Compile<NevermoreEmbeddedSqlExpressionAnalyzer>(code);
            AssertPassed(results);
        }
   }
}