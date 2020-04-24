using Microsoft.CodeAnalysis;

namespace Nevermore.Analyzers
{
    internal class Issue
    {
        public Issue(string message, SyntaxNode node)
        {
            Message = message;
            Node = node;
        }
        
        public string Message { get; }
        public SyntaxNode Node { get; }
    }
}