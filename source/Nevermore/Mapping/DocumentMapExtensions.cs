using System.Collections.Generic;

namespace Nevermore.Mapping
{
    internal static class DocumentMapExtensions
    {
        public static IEnumerable<string> GetColumnNames(this DocumentMap documentMap)
        {
            yield return documentMap.IdColumn!.ColumnName;

            foreach (var column in documentMap.Columns)
            {
                yield return column.ColumnName;
            }

            if (documentMap.HasJsonColumn())
            {
                yield return "JSON";
            }

            if (documentMap.HasJsonBlobColumn())
            {
                yield return "JSONBlob";
            }
        }
    }
}