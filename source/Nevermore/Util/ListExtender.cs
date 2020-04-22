using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Util
{
    internal static class ListExtender
    {
        // When using 'IN' clauses, the number of parameters results in lots of query plans. 
        //
        ///    (@p0 nvarchar(100))SELECT TOP 100 *  FROM dbo.[Customer]  WHERE ([FirstName] IN (@p0))  ORDER BY [Id]
        //     (@p0 nvarchar(100),@p1 nvarchar(100))SELECT TOP 100 *  FROM dbo.[Customer]  WHERE ([FirstName] IN (@p0, @p1))  ORDER BY [Id]
        // 
        // And so on. To speed reuse query plans, we pad the last value. If there are 7 clauses or 8 clauses, we pad it to 10, by repeating
        // the final value.
        public static void ExtendListRepeatingLastValue(List<object> inClauseValues)
        {
            if (inClauseValues.Count <= 5)
                // Optimize: no need to pad smaller queries as these will probably happen more often
                return;

            var finalSize = inClauseValues.Count;
            if (inClauseValues.Count <= 10) finalSize = 10;
            else if (inClauseValues.Count <= 15) finalSize = 15;
            else if (inClauseValues.Count <= 20) finalSize = 20;
            else if (inClauseValues.Count <= 30) finalSize = 30;
            else if (inClauseValues.Count <= 50) finalSize = 50;
            else if (inClauseValues.Count <= 75) finalSize = 75;
            else if (inClauseValues.Count <= 100) finalSize = 100;
            else if (inClauseValues.Count <= 130) finalSize = 130;
            else if (inClauseValues.Count <= 160) finalSize = 160;
            else if (inClauseValues.Count <= 200) finalSize = 200;
            else if (inClauseValues.Count <= 500) finalSize = 500;
            else if (inClauseValues.Count <= 1000) finalSize = 1000;

            if (inClauseValues.Count == finalSize)
                return;
            
            var last = inClauseValues.Last();
            var toPad = finalSize - inClauseValues.Count;
            for (var i = 0; i < toPad; i++)
            {
                inClauseValues.Add(last);
            }
        }
    }
}