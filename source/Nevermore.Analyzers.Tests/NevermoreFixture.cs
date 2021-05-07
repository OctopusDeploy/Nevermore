using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
    public abstract class NevermoreFixture
    {
	    protected static void AssertError(List<Diagnostic> results, string error)
        {
	        results.Count.Should().Be(1);
	        results[0].GetMessage().Should().Contain(error);
        }

        protected static void AssertPassed(List<Diagnostic> results)
        {
	        if (results.Count == 0)
		        return;

	        var errors = results.Select(r => r.GetMessage());
	        Assert.Fail(string.Join(Environment.NewLine, errors));
        }
    }
}