using System.Data;
using System.Linq;
using System.Text;
using Nevermore.Mapping;
using Nevermore.Util;

namespace Nevermore.IntegrationTests
{
    public class SchemaGenerator
    {
        public static void WriteTableSchema(DocumentMap mapping, string tableNameOverride, StringBuilder result)
        {
            var tableName = tableNameOverride ?? mapping.TableName;
            result.AppendLine("CREATE TABLE [" + tableName + "] (");
            result.AppendFormat("  [Id] NVARCHAR(50) NOT NULL CONSTRAINT [PK_{0}_Id] PRIMARY KEY CLUSTERED, ", tableName).AppendLine();

            foreach (var column in mapping.WritableIndexedColumns())
            {
                result.AppendFormat("  [{0}] {1} {2}, ", column.ColumnName, GetDatabaseType(column).ToUpperInvariant(), column.IsNullable ? "NULL" : "NOT NULL").AppendLine();
            }

            result.AppendFormat("  [JSON] NVARCHAR(MAX) NOT NULL").AppendLine();

            result.AppendLine(")");

            foreach (var unique in mapping.UniqueConstraints)
            {
                result.AppendFormat("ALTER TABLE [{0}] ADD CONSTRAINT [UQ_{1}] UNIQUE({2})", tableName,
                    unique.ConstraintName,
                    string.Join(", ", unique.Columns.Select(ix => "[" + ix + "]"))).AppendLine();
            }

            foreach (var referencedDocumentMap in mapping.RelatedDocumentsMappings)
            {
                var refTblName = referencedDocumentMap.TableName;
                result.AppendLine($"IF NOT EXISTS (SELECT name from sys.tables WHERE name = '{refTblName}')");
                result.AppendLine($"    CREATE TABLE [{refTblName}] (");
                result.AppendLine($"        [{referencedDocumentMap.IdColumnName}] nvarchar(50) NOT NULL,");
                result.AppendLine($"        [{referencedDocumentMap.IdTableColumnName}] nvarchar(50) NOT NULL,");
                result.AppendLine($"        [{referencedDocumentMap.RelatedDocumentIdColumnName}] nvarchar(50) NOT NULL,");
                result.AppendLine($"        [{referencedDocumentMap.RelatedDocumentTableColumnName}] nvarchar(50) NOT NULL ");
                result.AppendLine("    )");
            }
        }

        static string GetDatabaseType(ColumnMapping column)
        {
            var dbType = column.DbType;

            switch (dbType)
            {
                case DbType.AnsiString:
                    return "varchar" + GetLength(column);
                case DbType.AnsiStringFixedLength:
                    return "char";
                case DbType.Binary:
                    return "varbinary(max)";
                case DbType.Byte:
                    return "tinyint";
                case DbType.Boolean:
                    return "bit";
                case DbType.Currency:
                    return "decimal";
                case DbType.Date:
                    return "date";
                case DbType.DateTime:
                    return "datetime";
                case DbType.Decimal:
                    return "decimal";
                case DbType.Double:
                    return "float";
                case DbType.Guid:
                    return "uniqueidentifier";
                case DbType.Int16:
                    return "smallint";
                case DbType.Int32:
                    return "int";
                case DbType.Int64:
                    return "bigint";
                case DbType.SByte:
                    break;
                case DbType.Single:
                    break;
                case DbType.StringFixedLength:
                    return "nchar";
                case DbType.String:
                    return "nvarchar" + GetLength(column);
                case DbType.Time:
                    return "time";
                case DbType.DateTime2:
                case DbType.DateTimeOffset:
                    return "datetimeoffset";
            }

            return "??TYPE??";
        }
        static string GetLength(ColumnMapping column)
        {
            var length = column.MaxLength;
            if (length <= 0)
            {
                return "(??LENGTH??)";
            }

            if (length == 1)
            {
                return "";
            }

            if (length == int.MaxValue)
            {
                return "(MAX)";
            }

            return "(" + length + ")";
        }

    }
}
