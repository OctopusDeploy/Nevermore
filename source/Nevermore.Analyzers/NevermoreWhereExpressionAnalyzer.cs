using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nevermore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NevermoreWhereExpressionAnalyzer : DiagnosticAnalyzer
    {
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(AnalyzeCompilation);
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

                if (!(methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Nevermore") || methodSymbol.ContainingType.Name == "QueryBuilderWhereExtensions" || methodSymbol.ContainingType.Name == "DeleteQueryBuilderExtensions"))
                    return;

                if (invocation.ArgumentList.Arguments.Count != 1)
                    return;

                var expressionArgument = invocation.ArgumentList.Arguments[0].Expression as SimpleLambdaExpressionSyntax;
                if (expressionArgument == null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.NV0001NevermoreWhereExpressionError,
                            invocation.GetLocation(), "Cannot translate expression argument: " + invocation.ArgumentList));
                    return;
                }

                var result = expressionArgument.Body.Accept(new NevermoreWhereExpressionVisitor(expressionArgument.Parameter, context.SemanticModel));
                if (result != null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Descriptors.NV0001NevermoreWhereExpressionError,
                            result.Location ?? invocation.GetLocation(),
                            result.Message));
                }

            }, SyntaxKind.InvocationExpression);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.NV0001NevermoreWhereExpressionError);

    }
}