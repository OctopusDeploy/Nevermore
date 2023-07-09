using Microsoft.CodeAnalysis;

namespace Nevermore.Analyzers
{
    public class Issue
    {
        public Issue(string message, Location location)
        {
            Message = message;
            Location = location;
        }
        
        public string Message { get; }
        public Location Location { get; }
    }
}