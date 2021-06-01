using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Nevermore.Analyzers.Tests
{
    public class CodeCompiler
    {
        static string CodeWrapperTemplate = @"
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore;
using Nevermore.Querying;
using Nevermore.Advanced;

class Customer
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int? Age { get; set; }

    public const string Constant = ""ConstantValue"";

    public class Attributes {
        public const string Constant = ""ConstantValue"";
    }
}

class Program 
{
    public static void Main()
    {
        var transaction = (IRelationalTransaction)null;
        <CODE>
    }
}
";

        public static List<Diagnostic> Compile<TDiagnostic>(string code) where TDiagnostic : DiagnosticAnalyzer, new()
        {
            var project = CreateProject(new string[] { code });
            var compilation = project.GetCompilationAsync().Result;
            var diagnostics = compilation.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).ToList();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                throw new Exception("Compilation exception: " + string.Join(Environment.NewLine, diagnostics.Select(d => d.GetMessage())));

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TDiagnostic());
            var withAnalyzers = compilation.WithAnalyzers(analyzers);
            var results = withAnalyzers.GetAnalyzerDiagnosticsAsync(analyzers, CancellationToken.None).Result;
            return results.ToList();
        }

        static Project CreateProject(IEnumerable<string> sources)
        {
            var projectId = ProjectId.CreateNewId("TestProject");

            var references = new List<MetadataReference>();
            FindReferencesRecure(typeof(IRelationalTransaction).Assembly, new HashSet<string>(), references);

            var solution = new AdhocWorkspace()
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddMetadataReferences(projectId, references);

            var count = 0;
            foreach (var source in sources)
            {
                var newFileName = $"Foo{count}.cs";
                var documentId = DocumentId.CreateNewId(projectId, newFileName);
                var text = SourceText.From(CodeWrapperTemplate.Replace("<CODE>", source.Replace("'", "\"")));
                solution = solution.AddDocument(documentId, newFileName, text);
                count++;
            }


            return solution.GetProject(projectId);
        }

        static void FindReferencesRecure(Assembly assembly, HashSet<string> seenBefore, List<MetadataReference> results)
        {
            if (assembly == null)
                return;

            var metadata = MetadataReference.CreateFromFile(assembly.Location);
            results.Add(metadata);

            var references = assembly.GetReferencedAssemblies();
            foreach (var reference in references)
            {
                if (!seenBefore.Contains(reference.FullName))
                {
                    seenBefore.Add(reference.FullName);

                    var referencedAssembly = Assembly.Load(reference);
                    FindReferencesRecure(referencedAssembly, seenBefore, results);
                }
            }
        }

        static PortableExecutableReference GetAssemblyReference(IEnumerable<AssemblyName> assemblies, string name) => MetadataReference.CreateFromFile(Assembly.Load(assemblies.First(n => n.Name == name)).Location);
    }
}