using System;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Nevermore.Diagnostics
{
    internal static class QueryPlanThrashingDetector
    {
        readonly static ConcurrentDictionary<string, string> NormalizedQueries = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        readonly static Regex ReplacerRegex = new Regex(@"@\w+", RegexOptions.Compiled); 
        
        public static void Detect(string statement)
        {
            var key = ReplacerRegex.Replace(statement, "PARAM").ToLowerInvariant().Trim();
            if (NormalizedQueries.TryGetValue(key, out var existingStatement))
            {
                if (existingStatement != statement)
                {
                    throw new DuplicateQueryException($"Detected a SQL query that is otherwise a perfect duplicate of another query, except with different parameter names. This is likely to create thrashing of the query plan cache.\r\n\r\nThe statement being executed this time was: \r\n\r\n{statement}\r\n\r\nThe statement executed last time was:\r\n\r\n{existingStatement}\r\n\r\nRewrite your query to use more predictable parameter names, as this will allow the database to re-use the query plan for both queries.");
                }
            }

            // Two threads could potentially do this at once, but it's not that important - we'll detect it eventually
            NormalizedQueries.TryAdd(key, statement);
        }
    }
}