using NUnit.Framework;

namespace Nevermore.Analyzers.Tests;

public class NevermoreQueryableAnalyzerFixture : NevermoreFixture
{
	[Test]
	public void ShouldPassOnNonQueryableMethods()
	{
		var code = @"
			transaction.Queryable<Customer>().ToList();
			transaction.Queryable<Customer>();
		";

		var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
		AssertPassed(results);
	}

    [Test]
    public void ShouldPassForWhere()
    {
        var code = @"
			transaction.Queryable<Customer>().Where(c => c.FirstName == '').ToList();
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForOrderBy()
    {
        var code = @"
			transaction.Queryable<Customer>().OrderBy(c => c.FirstName).ToList();
			transaction.Queryable<Customer>().OrderByDescending(c => c.FirstName).ToList();
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForThenBy()
    {
        var code = @"
			transaction.Queryable<Customer>().OrderBy(c => c.FirstName).ThenBy(c => c.LastName).ToList();
			transaction.Queryable<Customer>().OrderBy(c => c.FirstName).ThenByDescending(c => c.LastName).ToList();
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForFirst()
    {
        var code = @"
			transaction.Queryable<Customer>().First(c => c.FirstName == ""Foo"");
			transaction.Queryable<Customer>().FirstOrDefault(c => c.FirstName == ""Foo"");
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForAny()
    {
        var code = @"
			transaction.Queryable<Customer>().Any();
			transaction.Queryable<Customer>().Any(c => c.FirstName == ""Foo"");
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForCount()
    {
        var code = @"
			transaction.Queryable<Customer>().Count();
			transaction.Queryable<Customer>().Count(c => c.FirstName == ""Foo"");
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForTake()
    {
        var code = @"
			transaction.Queryable<Customer>().Take(2);
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldPassForSkip()
    {
        var code = @"
			transaction.Queryable<Customer>().Skip(1);
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertPassed(results);
    }

    [Test]
    public void ShouldFailForSingle()
    {
        var code = @"
			transaction.Queryable<Customer>().Single(c => c.FirstName == 'Bob');
			transaction.Queryable<Customer>().SingleOrDefault(c => c.FirstName == 'Bob');
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertErrors(
            results,
            "Nevermore Queryable does not support Single",
            "Nevermore Queryable does not support SingleOrDefault");
    }

    [Test]
    public void ShouldFailForDistinct()
    {
        var code = @"
			transaction.Queryable<Customer>().Distinct();
			transaction.Queryable<Customer>().DistinctBy(c => c.FirstName);
		";

        var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
        AssertErrors(
            results,
            "Nevermore Queryable does not support Distinct",
            "Nevermore Queryable does not support DistinctBy");
    }

    [Test]
    public void ShouldPassOnQueryableFromSomewhereElse()
    {
	    var code = @"
			var list = new List<Customer>();
			list.AsQueryable().Select(c => c.FirstName);
		";

	    var results = CodeCompiler.Compile<NevermoreQueryableAnalyzer>(code);
	    AssertPassed(results);
    }
}