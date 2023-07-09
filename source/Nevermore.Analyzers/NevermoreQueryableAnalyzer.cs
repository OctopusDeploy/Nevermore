using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using LanguageNames = Microsoft.CodeAnalysis.LanguageNames;

namespace Nevermore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NevermoreQueryableAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.NV0002NevermoreQueryableError);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(DontUse_UnsupportedQueryableMethods, SyntaxKind.InvocationExpression);
    }

    void DontUse_UnsupportedQueryableMethods(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not InvocationExpressionSyntax {Expression: MemberAccessExpressionSyntax memberAccessExpressionSyntax} invocationExpressionSyntax)
        {
            return;
        }
        
        var symbolInfo = ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpressionSyntax);
        if (symbolInfo.Symbol is null)
            return;

        if (symbolInfo.Symbol.ContainingType.AllInterfaces.Any(i => i.Name == nameof(IQueryable)))
            return;

        var parentSymbolInfo = ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpressionSyntax.Expression);
        if (parentSymbolInfo.Symbol is not IMethodSymbol methodSymbol || 
            !methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Nevermore"))
            return;

        var result = invocationExpressionSyntax.Accept(new NevermoreQueryableVisitor());
        if (result != null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    Descriptors.NV0002NevermoreQueryableError,
                    result.Location ?? invocationExpressionSyntax.GetLocation(),
                    result.Message));
        }
    }
}