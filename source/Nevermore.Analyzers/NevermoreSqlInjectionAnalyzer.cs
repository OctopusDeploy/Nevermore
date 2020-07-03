using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nevermore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NevermoreSqlInjectionAnalyzer : DiagnosticAnalyzer
    {
        readonly HashSet<string> methodsWeCareAbout = new HashSet<string> {"Where", "Stream"};
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(AnalyzeCompilation);
        }
        
        void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterCodeBlockStartAction<SyntaxKind>(context =>
            {
                var ignoreThisBlock = false;

                var errors = new List<Diagnostic>();
                
                context.RegisterSyntaxNodeAction(invocationContext =>
                {
                    var invocation = (InvocationExpressionSyntax)invocationContext.Node;
                    ProcessInvocation(invocation, invocationContext, errors);
                }, SyntaxKind.InvocationExpression);

                context.RegisterCodeBlockEndAction(c =>
                {
                    if (ignoreThisBlock)
                        return;
                    
                    foreach (var error in errors)
                    {
                        c.ReportDiagnostic(error);
                    }
                });
            });
        }

        void ProcessInvocation(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, List<Diagnostic> errors)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
                return;

            var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;
            
            if (!methodsWeCareAbout.Contains(methodSymbol.Name))
                return;
            
            if (methodSymbol.ContainingType == null)
                return;

            if (!(methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Nevermore") || methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Querying")  || methodSymbol.ContainingType.Name.StartsWith("IQueryBuilder") || methodSymbol.ContainingType.Name == "QueryBuilderWhereExtensions" || methodSymbol.ContainingType.Name == "DeleteQueryBuilderExtensions"))
                return;

            if (methodSymbol.Name == null)
                return;

            if (invocation.ArgumentList.Arguments.Count < 1)
                return;
            
            var location = CheckForSqlInjection(invocation.ArgumentList.Arguments[0].Expression, context.SemanticModel);
            if (location != Location.None)
            {
                errors.Add(Diagnostic.Create(ErrorDescriptor, location, "This expression uses string concatenation, which creates a risk of a SQL Injection vulnerability. Pass parameters or arguments instead. If you're absolutely sure it's safe, use '#pragma warning disable NV0007' plus a comment explaining why."));
            }
        }

        static Location CheckForSqlInjection(ExpressionSyntax expression, SemanticModel model)
        {
            if (expression is BinaryExpressionSyntax binaryExpressionSyntax)
            {
                if (binaryExpressionSyntax.OperatorToken.Text == "+")
                {
                    return expression.GetLocation();
                }
            }
            
            if (expression is InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax)
            {
                return expression.GetLocation();
            }

            return Location.None;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ErrorDescriptor);
        
        static readonly DiagnosticDescriptor ErrorDescriptor = new DiagnosticDescriptor("NV0007", "Nevermore SQL injection", "{0}", "Nevermore", DiagnosticSeverity.Error, true, helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");
    }
}