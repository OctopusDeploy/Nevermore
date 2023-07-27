namespace Nevermore
{
    public static class LikeComparisonExtensions
    {
        public static string EscapeForLikeComparison(this string value)
        {
            return value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
        }
    }
}