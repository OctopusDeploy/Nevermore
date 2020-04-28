using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public class NevermoreEmbeddedSqlExpressionAnalyzer : DiagnosticAnalyzer
    {
        readonly HashSet<string> methodsWeCareAbout = new HashSet<string> {"Where", "Parameter", "LikeParameter", "LikePipedParameter", "ToList", "Stream", "First", "FirstOrDefault", "Take"};
        
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(AnalyzeCompilation);
        }

        class QueryAnalysisUnit
        {
            static Regex parameterDetectionRegex = new Regex(@"@\w+", RegexOptions.Compiled);
            StringBuilder queryBuilder = new StringBuilder();
            HashSet<string> actualParameters = new HashSet<string>();
            List<SyntaxNode> nodes = new List<SyntaxNode>();

            public void AddQueryText(string query, SyntaxNode node)
            {
                queryBuilder.AppendLine(query);
                nodes.Add(node);
            }

            public void AddSuppliedParameter(string name)
            {
                var values = parameterDetectionRegex.Matches(name).Select(m => m.Value.TrimStart('@')).ToList();
                if (values.Count == 0)
                {
                    actualParameters.Add(name.TrimStart('@'));
                }
                else
                {
                    foreach (var value in values)
                    {
                        actualParameters.Add(value.TrimStart('@'));
                    }
                }
            }

            public (bool Valid, string Message, DiagnosticDescriptor DiagnosticDescriptor, SyntaxNode node) Validate()
            {
                var query = queryBuilder.ToString();
                if (string.IsNullOrWhiteSpace(query))
                    return (true, null, null, null);
                
                var expectedParameters = parameterDetectionRegex.Matches(query).Select(m => m.Value.TrimStart('@')).ToList();
                
                var notSupplied = expectedParameters.Where(p => !actualParameters.Contains(p)).ToArray();
                if (notSupplied.Length == 0)
                    return (true, null, null, null);

                var suppliedMessage = actualParameters.Count == 0 ? "Did not detect any parameters supplied." : "Detected the following parameters: " + string.Join(", ", actualParameters.Select(p => "@" + p));

                // ReSharper disable once PossibleUnintendedLinearSearchInSet because it's intentional
                var notSuppliedIfCasingIgnored = expectedParameters.Where(p => !actualParameters.Contains(p, StringComparer.OrdinalIgnoreCase)).ToArray();
                
                if (notSuppliedIfCasingIgnored.Length == 0)
                {
                    if (notSupplied.Length == 1)
                        return (false, $"The query refers to the parameter '@{notSupplied[0]}', but the parameter being passed uses different casing. Change the call to '.Parameter(\"{notSupplied[0]}\", ...)' to correct the casing. " + suppliedMessage, ErrorDescriptor, nodes.First());

                    return (false, $"The following parameters appear in the SQL query string, but the values have been passed using inconsistently cased names. Inconsistent parameter names: {string.Join(", ", notSupplied)}. " + suppliedMessage, ErrorDescriptor, nodes.First());
                }

                if (notSupplied.Length == 1)
                    return (false, $"The query refers to the parameter '@{notSupplied[0]}', but no value for the parameter is being passed to the query. Make sure you add a call to '.Parameter(\"{notSupplied[0]}\", ...)' to the query. " + suppliedMessage, ErrorDescriptor, nodes.First());

                return (false, $"The following parameters appear in the SQL query string, but have not been passed to the query as parameters. Check the spelling and try again. Missing parameters: {string.Join(", ", notSupplied)}. " + suppliedMessage, ErrorDescriptor, nodes.First());
            }
        }
        
        void AnalyzeCompilation(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterCodeBlockStartAction<SyntaxKind>(context =>
            {
                // We'll capture data about every query
                var discovered = new ConcurrentDictionary<SyntaxNode, QueryAnalysisUnit>();

                context.RegisterSyntaxNodeAction(invocationContext =>
                {
                    var invocation = (InvocationExpressionSyntax)invocationContext.Node;
                    ProcessInvocation(invocation, invocationContext, discovered);
                }, SyntaxKind.InvocationExpression);
                
                context.RegisterCodeBlockEndAction(analysisContext =>
                {
                    foreach (var (node, unit) in discovered)
                    {
                        var (success, message, level, syntaxNode) = unit.Validate();

                        if (!success)
                        {
                            analysisContext.ReportDiagnostic(Diagnostic.Create(level, syntaxNode.GetLocation(), message));
                        }
                    }
                });
            });
        }

        void ProcessInvocation(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext context, ConcurrentDictionary<SyntaxNode, QueryAnalysisUnit> discovered)
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
            
            // A query might look like this:
            //   transaction.Query<Customer>()
            //      .Where("FirstName = @name")
            //      .Where("LastName = @last")
            //      .Parameter("@name", "foo")
            //      .ToList();
            // We need the whole chain of methods being called, and some of them are extension methods, others are 
            // normal methods. So instead, each time we see a method we care about (Where, Parameter, ToList...) 
            // we .Parent our way up the chain until we hit the topmost invocation. Then we use that as a key, 
            // so that all other methods off that root are collected together into one "analysis unit". 
            var startOfMethodChain = FindStartOfMethodChain(invocation);
            if (startOfMethodChain == null)
                return;

            var currentQuery = discovered.GetOrAdd(startOfMethodChain, _ => new QueryAnalysisUnit());
            
            if (methodSymbol.Name == "Where")
            {
                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    return;

                if (invocation.ArgumentList.Arguments.Count != 1)
                    return;

                var query = GetStringValue(invocation.ArgumentList.Arguments[0].Expression, context.SemanticModel);
                currentQuery.AddQueryText(query, invocation.ArgumentList.Arguments[0].Expression);
            }
            else if (methodSymbol.Name == "Stream")
            {
                if (methodSymbol.MethodKind != MethodKind.Ordinary)
                    return;

                if (invocation.ArgumentList.Arguments.Count < 1)
                    return;

                var query = GetStringValue(invocation.ArgumentList.Arguments[0].Expression, context.SemanticModel);
                currentQuery.AddQueryText(query, invocation.ArgumentList.Arguments[0].Expression);
                
                if (invocation.ArgumentList.Arguments.Count < 2)
                    return;

                FindCommandValueParameters(invocation.ArgumentList.Arguments[1].Expression,
                    context.SemanticModel, currentQuery);
            }
            else if (methodSymbol.Name == "Parameter" || methodSymbol.Name == "LikeParameter" || methodSymbol.Name == "LikePipedParameter")
            {
                if (invocation.ArgumentList.Arguments.Count < 1)
                    return;
                
                var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                var firstArgValue = GetStringValue(firstArg, context.SemanticModel);
                if (firstArgValue != null)
                    currentQuery.AddSuppliedParameter(firstArgValue);
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(ErrorDescriptor, invocation.GetLocation(), "Could not understand parameter: " + firstArg));
                }
            }
        }

        void FindCommandValueParameters(ExpressionSyntax expression, SemanticModel model, QueryAnalysisUnit currentQuery)
        {
            var value = GetStringValue(expression, model);
            currentQuery.AddSuppliedParameter(value);
        }

        static SyntaxNode FindStartOfMethodChain(SyntaxNode subject)
        {
            SyntaxNode start = null;
            var parent = subject;
            while (parent is MemberAccessExpressionSyntax || parent is InvocationExpressionSyntax)
            {
                start = parent;
                parent = parent.Parent;
            }
            
            return start;
        }

        static string GetStringValue(ExpressionSyntax expression, SemanticModel model)
        {
            if (expression is LiteralExpressionSyntax queryAsLiteral)
            {
                return queryAsLiteral.Token.ValueText;
            }

            if (expression is IdentifierNameSyntax queryAsIdentifier)
            {
                var symbol = model.GetSymbolInfo(queryAsIdentifier, CancellationToken.None);

                if (symbol.Symbol is ILocalSymbol local)
                {
                    var references = local.DeclaringSyntaxReferences;
                    if (references.Length != 1)
                        return null;
                    
                    var declaration = references[0].GetSyntax();
                    if (declaration is VariableDeclaratorSyntax variableDeclaratorSyntax)
                    {
                        return GetStringValue(variableDeclaratorSyntax.Initializer.Value, model);
                    }
                }
                
                if (symbol.Symbol is IPropertySymbol propertySymbol)
                {
                    var references = propertySymbol.DeclaringSyntaxReferences;
                    if (references.Length != 1)
                        return null;

                    return propertySymbol.Name;
                }
            }

            if (expression is InterpolatedStringExpressionSyntax interpolatedStringExpressionSyntax)
            {
                return string.Concat(interpolatedStringExpressionSyntax.Contents.Select(c => GetInterpolatedText(c, model)));
            }

            if (expression is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var symbol = model.GetSymbolInfo(invocationExpressionSyntax);
                if (symbol.Symbol == null)
                {
                    if (invocationExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax)
                    {
                        if (identifierNameSyntax.Identifier.Text == "nameof")
                        {
                            if (invocationExpressionSyntax.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax
                                nameofArgIdentfierNameSyntax)
                            {
                                return nameofArgIdentfierNameSyntax.Identifier.Text;
                            }
                        }
                    }
                }
                if (symbol.Symbol is IMethodSymbol methodSymbol)
                {
                    return methodSymbol.Name;
                }
            }

            if (expression is ObjectCreationExpressionSyntax creationExpressionSyntax)
            {
                var typeInfo = model.GetSymbolInfo(creationExpressionSyntax.Type);

                if (typeInfo.Symbol is ITypeSymbol typeSymbol)
                {
                    if (typeSymbol.Name == "Parameter" && creationExpressionSyntax.ArgumentList.Arguments.Count > 0)
                    {
                        return GetStringValue(creationExpressionSyntax.ArgumentList.Arguments[0].Expression, model);
                    }
                    
                    if (typeSymbol.Name == "CommandParameterValues")
                    {
                        var values = new List<string>();
                        if (creationExpressionSyntax.Initializer != null && creationExpressionSyntax.Initializer.Expressions.Count > 0)
                        {
                            values.AddRange(creationExpressionSyntax.Initializer.Expressions.Select(e => GetStringValue(e, model)));
                        }

                        if (creationExpressionSyntax.ArgumentList != null && creationExpressionSyntax.ArgumentList.Arguments.Count > 0)
                        {
                            values.AddRange(creationExpressionSyntax.ArgumentList.Arguments.Select(e => GetStringValue(e.Expression, model)));
                        }
                        
                        return string.Join(", ", values.Select(v => '@' + v.TrimStart('@')));
                    }

                    return "???? " + typeSymbol.Name;
                }
            }
            
            if (expression is InitializerExpressionSyntax initializerExpressionSyntax)
            {
                return string.Join(", ", initializerExpressionSyntax.Expressions.Take(1).Select(e => "@" + GetStringValue(e, model).TrimStart('@')));
            }
            
            if (expression is AssignmentExpressionSyntax assignmentExpressionSyntax)
            {
                return GetStringValue(assignmentExpressionSyntax.Left, model);
            }
            
            if (expression is ImplicitElementAccessSyntax implicitElementAccessSyntax)
            {
                if (implicitElementAccessSyntax.ArgumentList.Arguments.Count >= 1)
                {
                    return GetStringValue(implicitElementAccessSyntax.ArgumentList.Arguments[0].Expression, model);
                }
            }
            
            if (expression is AnonymousObjectCreationExpressionSyntax anonymousObjectCreationExpressionSyntax)
            {
                return string.Join(", ", anonymousObjectCreationExpressionSyntax.Initializers.Select(e => "@" + (e.NameEquals == null ? e.Expression.ToString() : GetStringValue(e.NameEquals.Name, model)).TrimStart('@')));
            }
            
            return expression.GetType().Name + " -- " + expression;
        }

        static string GetInterpolatedText(InterpolatedStringContentSyntax contentSyntax, SemanticModel model)
        {
            if (contentSyntax is InterpolatedStringTextSyntax interpolatedStringTextSyntax)
            {
                return interpolatedStringTextSyntax.TextToken.Text;
            }
            
            if (contentSyntax is InterpolationSyntax interpolationSyntax)
            {
                return GetStringValue(interpolationSyntax.Expression, model);
            }

            return contentSyntax.GetType().Name;
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(WarningDescriptor, ErrorDescriptor);
        
        static readonly DiagnosticDescriptor WarningDescriptor = new DiagnosticDescriptor("NV0005", "Nevermore embedded SQL", "{0}", "Nevermore", DiagnosticSeverity.Warning, true, helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");
        static readonly DiagnosticDescriptor ErrorDescriptor = new DiagnosticDescriptor("NV0006", "Nevermore embedded SQL", "{0}", "Nevermore", DiagnosticSeverity.Error, true, helpLinkUri: "https://github.com/OctopusDeploy/Nevermore/wiki/Querying");
    }
}