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
        
        // Only target IQueryable methods
        var symbolInfo = ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpressionSyntax);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol ||
            !IsQueryableExtensionMethod(methodSymbol))
        {
            return;
        }

        // Only target calls to methods on Nevermore types that return an IQueryable.
        // E.g: IReadQueryExecutor.Queryable<T>().Select()
        //  not IQueryable<string> GetNames(IQueryable<T> queryable) => queryable.Select(c => c.Names)
        var parentSymbolInfo = ModelExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpressionSyntax.Expression);
        if (parentSymbolInfo.Symbol is not IMethodSymbol parentMethodSymbol || 
            !parentMethodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Nevermore"))
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

    bool IsQueryableExtensionMethod(IMethodSymbol methodSymbol)
    {
        // If the method is not defined in the Queryable class, then we don't care
        if (methodSymbol.ContainingType.Name != nameof(Queryable))
            return false;
            
        // If this is an extension method that doesn't take an IQueryable as it's receiver, then we don't care
        if (methodSymbol is {IsExtensionMethod: true, ReceiverType: not null} &&
            methodSymbol.ReceiverType.Interfaces.All(i => i.Name != nameof(IQueryable)))
        {
            return false;
        }

        return true;
    }
}