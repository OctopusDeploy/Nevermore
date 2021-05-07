using System.Collections.Generic;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Nevermore.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NevermoreDisposeLocalDeclarationTransactionAnalyzer : DiagnosticAnalyzer
    {
        readonly HashSet<string> methodsWeCareAbout = new HashSet<string> {"BeginTransaction", "BeginReadTransaction", "BeginWriteTransaction", "BeginReadTransactionAsync", "BeginWriteTransactionAsync"};

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.LocalDeclarationStatement);
        }

         void Handle(SyntaxNodeAnalysisContext context)
         {
             if (context.Node is LocalDeclarationStatementSyntax {Declaration: {Variables: var variables} localDeclaration} statement &&
                 statement.UsingKeyword.IsKind(SyntaxKind.None))
             {
                 foreach (var declarator in variables)
                 {
                     if (declarator.Initializer is {Value: { } syntax})
                     {
                         var expression = (syntax as InvocationExpressionSyntax)?.Expression ??
                                          (syntax as AwaitExpressionSyntax)?.Expression;

                         if (expression is { } &&
                             context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol is
                                 { } symbol &&
                             methodsWeCareAbout.Contains(symbol.Name))
                         {
                             context.ReportDiagnostic(Diagnostic.Create(
                                 Descriptors.NV0008NevermoreDisposableTransactionCreated,
                                 localDeclaration.GetLocation(), "Nevermore transaction is never disposed"));
                         }
                     }
                 }
             }
         }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.NV0008NevermoreDisposableTransactionCreated);
    }
}