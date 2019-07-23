namespace Nevermore.AST
{
    public static class Format
    {
        public static string IndentLines(string lines)
        {
            var indent = new string(' ', 4);
            return indent + lines.Replace("\n", "\n" + indent);
        }
    }
}