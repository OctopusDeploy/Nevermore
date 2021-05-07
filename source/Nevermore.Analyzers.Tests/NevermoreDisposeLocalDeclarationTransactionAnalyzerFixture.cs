using NUnit.Framework;

namespace Nevermore.Analyzers.Tests
{
    public class NevermoreDisposeLocalDeclarationTransactionAnalyzerFixture : NevermoreFixture
    {
        [Test]
        [TestCase("BeginTransaction")]
        [TestCase("BeginReadTransaction")]
        [TestCase("BeginWriteTransaction")]
        public void ShouldDetectTransactionNotBeingDisposed(string method)
        {
            var code = @$"
var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
var read = store.{method}(NevermoreDefaults.IsolationLevel);
var x = read.Load<object>('id');
";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertError(results, "Nevermore transaction is never disposed");
        }

        [Test]
        [TestCase("BeginReadTransactionAsync")]
        [TestCase("BeginWriteTransactionAsync")]
        public void ShouldDetectAsyncTransactionNotBeingDisposed(string method)
        {
            var code = $@"
async void M()
{{
    var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
    var read = await store.{method}(NevermoreDefaults.IsolationLevel);
    var x = read.Load<object>('id');
}}
";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertError(results, "Nevermore transaction is never disposed");
        }

        [Test]
        [TestCase("BeginTransaction")]
        [TestCase("BeginReadTransaction")]
        [TestCase("BeginWriteTransaction")]
        public void ShouldCompileIfTransactionIsDisposedWithUsingStatement(string method, bool isAsync = false)
        {
            var code = @$"
void Main()
{{
    var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
    using var read = store.{method}(NevermoreDefaults.IsolationLevel);
    var x = read.Load<object>('id');
}}";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertPassed(results);
        }

        [Test]
        [TestCase("BeginReadTransactionAsync")]
        [TestCase("BeginWriteTransactionAsync")]
        public void ShouldCompileIfAsyncTransactionIsDisposedWithUsingStatement(string method)
        {
            var code = $@"
async void M()
{{
    var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
    using var read = await store.{method}(NevermoreDefaults.IsolationLevel);
    var x = read.Load<object>('id');
}}
";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertPassed(results);
        }

        [Test]
        [TestCase("BeginTransaction")]
        [TestCase("BeginReadTransaction")]
        [TestCase("BeginWriteTransaction")]
        public void ShouldCompileIfTransactionIsDisposedWithUsingBlock(string method)
        {
            var code = @$"
var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
using(var read = store.{method}(NevermoreDefaults.IsolationLevel))
{{
    var x = read.Load<object>('id');
}}
";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertPassed(results);
        }

        [Test]
        [TestCase("BeginReadTransactionAsync")]
        [TestCase("BeginWriteTransactionAsync")]
        public void ShouldCompileIfAsyncTransactionIsDisposedWithUsingBlock(string method)
        {
            var code = $@"
async void M()
{{
    var store = new RelationalStore(new RelationalStoreConfiguration('connection'));
    using(var read = await store.{method}(NevermoreDefaults.IsolationLevel))
    {{
        var x = read.Load<object>('id');
    }}
}}
";

            var results = CodeCompiler.Compile<NevermoreDisposeLocalDeclarationTransactionAnalyzer>(code);
            AssertPassed(results);
        }
    }
}