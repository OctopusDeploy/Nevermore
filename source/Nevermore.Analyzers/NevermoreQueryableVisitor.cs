using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nevermore.Analyzers;

public class NevermoreQueryableVisitor : CSharpSyntaxVisitor<Issue>
{
    readonly string[] supportedQueryableMethods =
    {
        nameof(Queryable.Where),
        nameof(Queryable.OrderBy),
        nameof(Queryable.OrderByDescending),
        nameof(Queryable.ThenBy),
        nameof(Queryable.ThenByDescending),
        nameof(Queryable.First),
        nameof(Queryable.FirstOrDefault),
        nameof(Queryable.Any),
        nameof(Queryable.Count),
        nameof(Queryable.Take),
        nameof(Queryable.Skip),
        nameof(Queryable.Select)
    };
    
    public override Issue DefaultVisit(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccessExpressionSyntax } invocationExpressionSyntax)
        {
            return null;
        }

        if (supportedQueryableMethods.Contains(memberAccessExpressionSyntax.Name.Identifier.Text))
        {
            return null;
        }
        
        return new Issue(
            memberAccessExpressionSyntax.Name.Identifier.Text,
            invocationExpressionSyntax.GetLocation());
    }
}