using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.SymbolStore;
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
            compilationStartContext.RegisterCodeBlockStartAction<SyntaxKind>(
                context =>
                {
                    var ignoreThisBlock = false;

                    var errors = new List<Diagnostic>();

                    context.RegisterSyntaxNodeAction(
                        invocationContext =>
                        {
                            var invocation = (InvocationExpressionSyntax) invocationContext.Node;
                            ProcessInvocation(invocation, invocationContext, errors);
                        },
                        SyntaxKind.InvocationExpression
                    );

                    context.RegisterCodeBlockEndAction(
                        c =>
                        {
                            if (ignoreThisBlock)
                                return;

                            foreach (var error in errors)
                            {
                                c.ReportDiagnostic(error);
                            }
                        }
                    );
                }
            );
        }

        void ProcessInvocation(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, List<Diagnostic> errors)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
            if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
                return;

            var methodSymbol = (IMethodSymbol) symbolInfo.Symbol;

            if (!methodsWeCareAbout.Contains(methodSymbol.Name))
                return;

            if (methodSymbol.ContainingType == null)
                return;

            if (!(methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Nevermore") || methodSymbol.ContainingType.ContainingNamespace.Name.StartsWith("Querying") || methodSymbol.ContainingType.Name.StartsWith("IQueryBuilder") || methodSymbol.ContainingType.Name == "QueryBuilderWhereExtensions" || methodSymbol.ContainingType.Name == "DeleteQueryBuilderExtensions"))
                return;

            if (methodSymbol.Name == null)
                return;

            if (invocation.ArgumentList.Arguments.Count < 1)
                return;

            var location = CheckForSqlInjection(context, invocation.ArgumentList.Arguments[0].Expression, context.SemanticModel);
            if (location != Location.None)
            {
                errors.Add(Diagnostic.Create(Descriptors.NV0007NevermoreSqlInjectionError, location, "This expression uses string concatenation, which creates a risk of a SQL Injection vulnerability. Pass parameters or arguments instead. If you're absolutely sure it's safe, use '#pragma warning disable NV0007' plus a comment explaining why."));
            }
        }

        static Location CheckForSqlInjection(SyntaxNodeAnalysisContext context, ExpressionSyntax expression, SemanticModel model)
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
                if (interpolatedStringExpressionSyntax.Contents.All(c => IsStringLiteralOrNameOf(context, c)))
                    return Location.None;
                return expression.GetLocation();
            }

            return Location.None;
        }

        static bool IsStringLiteralOrNameOf(SyntaxNodeAnalysisContext context, InterpolatedStringContentSyntax content)
        {
            // The string being interpolated into
            if (content is InterpolatedStringTextSyntax)
                return true;


            if (content is InterpolationSyntax interpolationSyntax)
            {
                // nameof() invocation
                if (interpolationSyntax.Expression is InvocationExpressionSyntax invocation)
                    return invocation.Expression is IdentifierNameSyntax invocationIdentifier &&
                           invocationIdentifier.Identifier.Text == "nameof";

                // variable
                if (interpolationSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)
                {
                    if (context.SemanticModel.GetSymbolInfo(identifierNameSyntax).Symbol is ILocalSymbol symbol)
                    {
                        if (symbol.IsConst)
                            return true;

                        var symbolType = GetSymbolType(symbol);
                        if (IsTypeThatIsOkToConcatenate(symbolType))
                            return true;
                    }

                    return false;
                }

                // property
                if (interpolationSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                {
                    var symbol = context.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax).Symbol;
                    if (symbol is IFieldSymbol fieldSymbol && fieldSymbol.IsConst)
                        return true;

                    return false;
                }
            }

            return false;
        }

        static bool IsTypeThatIsOkToConcatenate(INamedTypeSymbol symbolType)
        {
            if (symbolType == null)
                return false;

            if (symbolType.EnumUnderlyingType != null)
                return true;

            switch (symbolType.SpecialType)
            {
                case SpecialType.System_Enum:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_DateTime:
                    return true;
                default:
                    return false;
            }
        }

        static INamedTypeSymbol GetSymbolType(ILocalSymbol symbol)
        {
            var type = symbol.Type as INamedTypeSymbol;
            if (type == null)
                return null;

            if (type.SpecialType == SpecialType.System_Nullable_T ||
                type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return type.TypeArguments[0] as INamedTypeSymbol;

            return type;
        }


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Descriptors.NV0007NevermoreSqlInjectionError);
    }
}