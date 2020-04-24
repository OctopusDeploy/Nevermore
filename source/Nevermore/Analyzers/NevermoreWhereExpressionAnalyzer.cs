using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Nevermore.Analyzers;
using Nevermore.Querying;

namespace Nevermore.Advanced
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NevermoreWhereExpressionAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                AnalyzeCompilation(compilationStartContext);
            });
        }

        void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(context =>
            {
                var invocation = (InvocationExpressionSyntax)context.Node; // RegisterSymbolAction guarantees by 2nd arg
                var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
                if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
                    return;

                var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
                
                if (methodSymbol.Name != "Where")
                    return;
                
                if (methodSymbol.MethodKind != MethodKind.ReducedExtension)
                    return;
                
                if (methodSymbol.ContainingType == null)
                    return;
                
                if (!(methodSymbol.ContainingType.Name == typeof(QueryBuilderWhereExtensions).Name || methodSymbol.ContainingType.Name == typeof(DeleteQueryBuilderExtensions).Name))
                    return;
                
                if (invocation.ArgumentList.Arguments.Count != 1)
                    return;

                var expressionArgument = invocation.ArgumentList.Arguments[0].Expression as SimpleLambdaExpressionSyntax;
                if (expressionArgument == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            MyDescriptor,
                            invocation.GetLocation(), invocation.ArgumentList));
                    return;
                }

                var result = expressionArgument.Body.Accept(new NevermoreWhereExpressionVisitor(expressionArgument.Parameter, context.SemanticModel));
                if (result != null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            MyDescriptor,
                            invocation.GetLocation(), result.Message));
                }

            }, SyntaxKind.InvocationExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(MyDescriptor);
        
        static DiagnosticDescriptor MyDescriptor = new DiagnosticDescriptor("NV0001", "Nevermore LINQ expression", "Nevermore LINQ support will not be able to translate this expression: {0}", "Design", DiagnosticSeverity.Error, true);
    }
}