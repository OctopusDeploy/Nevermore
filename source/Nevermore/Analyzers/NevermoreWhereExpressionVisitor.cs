using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nevermore.Analyzers
{
    internal class NevermoreWhereExpressionVisitor : CSharpSyntaxVisitor<Issue>
    {
        readonly ParameterSyntax expressionArgumentParameter;
        readonly SemanticModel model;

        readonly HashSet<string> understoodBinaryOperators = new HashSet<string>()
        {
            "&&",
            "==",
            "!=",
            ">",
            ">=",
            "<",
            "<=",
        };
        
        readonly HashSet<string> understoodStringMethods = new HashSet<string>()
        {
            "StartsWith", "EndsWith", "Contains"
        };
        
        readonly HashSet<string> understoodNormalMethods = new HashSet<string>()
        {
            "In", "NotIn"
        };
        
        readonly HashSet<string> understoodYodaMethods = new HashSet<string>()
        {
            "Contains"
        };
         
        public NevermoreWhereExpressionVisitor(ParameterSyntax expressionArgumentParameter, SemanticModel model)
        {
            this.expressionArgumentParameter = expressionArgumentParameter;
            this.model = model;
        }
        
        public override Issue DefaultVisit(SyntaxNode node)
        {
            if (node is BinaryExpressionSyntax binaryExpressionSyntax)
            {
                var operatorToken = binaryExpressionSyntax.OperatorToken.Text;
                if (operatorToken == "&&")
                {
                    return DefaultVisit(binaryExpressionSyntax.Left)
                           ?? DefaultVisit(binaryExpressionSyntax.Right);
                }
                
                if (!understoodBinaryOperators.Contains(operatorToken))
                    return new Issue($"Cannot translate token \"{operatorToken}\". Only the following tokens are understood: " + string.Join(", ", understoodBinaryOperators.Select(s => "\"" + s + "\"")), node);

                return DefaultVisit(binaryExpressionSyntax.Left);
            }
            
            if (node is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                var symbolInfo = model.GetSymbolInfo(invocationExpressionSyntax);
                if (symbolInfo.Symbol?.Kind != SymbolKind.Method)
                    return new Issue("Cannot translate: " + invocationExpressionSyntax.Expression, node);

                var methodSymbol = (IMethodSymbol)symbolInfo.Symbol;

                if (methodSymbol.ContainingType == null)
                    return new Issue("Cannot translate method  " + invocationExpressionSyntax, invocationExpressionSyntax);

                // We don't care about the arguments to the method, as Nevermore calls Compile() to get the results. So we just need to make sure the left
                // (the thing being called on) is a property. "Yoda methods" are the opposite, like "myList.Contains(c.FirstName)", where we care about the 
                // first argument, not about the thing it's being called on.
                SyntaxNode mustBeProperty;
                if (understoodStringMethods.Contains(methodSymbol.Name) && methodSymbol.ContainingType.Name == "String")
                {
                    mustBeProperty = invocationExpressionSyntax.Expression;
                }
                else if (understoodNormalMethods.Contains(methodSymbol.Name))
                {
                    mustBeProperty = invocationExpressionSyntax.Expression;
                }
                else if (understoodYodaMethods.Contains(methodSymbol.Name))
                {
                    // Can't figure out how to analyze this yet, ideally we would check if the argument list contains
                    // a property only.
                    return null;
                }
                else
                {
                    return new Issue($"Cannot translate call to method '{methodSymbol.Name}'. Nevermore can only translate: " + string.Join(", ", understoodNormalMethods.Concat(understoodYodaMethods).Concat(understoodStringMethods).Distinct().Select(s => "\"" + s + "\"")), node);
                }
                
                var me = mustBeProperty as MemberAccessExpressionSyntax;
                if (me != null)
                    return DefaultVisit(me.Expression);

                return new Issue($"Unexpected expression ({mustBeProperty?.GetType().Name}): " + mustBeProperty, node);   
            }
            
            if (node is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                if (memberAccessExpressionSyntax.Expression.ToString() != expressionArgumentParameter.Identifier.Text)
                {
                    return new Issue("Property accessors can only be on the left side of an expression, and only a property on the document being queried. " + memberAccessExpressionSyntax.Expression, node);
                }
                
                var symbolInfo = model.GetSymbolInfo(memberAccessExpressionSyntax);
                var propertySymbol = symbolInfo.Symbol as IPropertySymbol;
                if (propertySymbol == null)
                {
                    return new Issue("You can only query against public properties", node);
                }
                
                if (propertySymbol.GetMethod == null)
                {
                    return new Issue("You can only query against public properties with getters", node);
                }

                // OK!
                return null;
            }

            if (node is PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax)
            {
                if (prefixUnaryExpressionSyntax.OperatorToken.Text != "!")
                {
                    return new Issue("Cannot translate token " + prefixUnaryExpressionSyntax.OperatorToken.Text, node);
                }

                return DefaultVisit(prefixUnaryExpressionSyntax.Operand);
            }

            return new Issue($"Cannot understand expression ({node.GetType().Name}) " + node, node);
        }
    }
}