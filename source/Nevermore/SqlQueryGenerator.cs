using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nevermore.Joins;

namespace Nevermore
{
    public class SqlQueryGenerator : IQueryGenerator
    {
        readonly StringBuilder queryText = new StringBuilder();

        string[] orderByFields = new string[0];

        string tableHint;
        readonly ITableAliasGenerator tableAliasGenerator;
        readonly CommandParameters queryParameters = new CommandParameters();
        private bool defaultOrderBy = true;


        public SqlQueryGenerator(string viewOrTableName, ITableAliasGenerator tableAliasGenerator = null)
        {
            this.tableAliasGenerator = tableAliasGenerator ?? new TableAliasGenerator();
            this.ViewOrTableName = viewOrTableName;
        }

        public CommandParameters QueryParameters
        {
            get
            {
                var all = new CommandParameters(queryParameters);
                foreach (var join in Joins)
                    all.AddRange(join.RightQuery.QueryParameters);
                return all;
            }
        }

        public string ViewOrTableName { get; private set; }
        public IList<IJoin> Joins { get; } = new List<IJoin>();

        string GetOrderByClause(string tableAlias = null)
        {
            var fields = orderByFields;
            if (fields.Length == 0 && defaultOrderBy)
                fields = new[] {"[Id]"};

            if (!string.IsNullOrEmpty(tableAlias))
                fields = fields.Select(t => $"{tableAlias}.{t}").ToArray();

            return fields.Any()
                ? "ORDER BY " + string.Join(", ", fields)
                : "";
        }

        string GetWhereClause()
        {
            return queryText.Length > 0
                ? "WHERE " + queryText
                : string.Empty;
        }

        public override string ToString()
        {
            return GetWhereClause() + " " + GetOrderByClause();
        }

        public void UseTable(string tableName)
        {
            ViewOrTableName = tableName;
        }

        public void UseView(string viewName)
        {
            ViewOrTableName = viewName;
        }


        public void UseHint(string hintClause)
        {
            tableHint = hintClause;
        }

        public string CountQuery()
        {
            if (Joins.Any())
                return GetClausesForJoinQuery("SELECT COUNT(*)", false);

            return $"SELECT COUNT(*) " + GetClauses(false);
        }


        public string TopQuery(int top = 1)
        {
            if (Joins.Any())
                return GetClausesForJoinQuery($"SELECT TOP {top} {{0}}.*", true);

            return $"SELECT TOP {top} * " + GetClauses(true);
        }

        public string SelectQuery(bool orderBy = true)
        {
            if (Joins.Any())
                return GetClausesForJoinQuery("SELECT {0}.*", orderBy);

            return $"SELECT * " + GetClauses(orderBy);
        }


        public string PaginateQuery(int skip, int take)
        {
            AddParameter("_minrow", skip + 1);
            AddParameter("_maxrow", take + skip);
            var select = GetClausesForJoinQuery($"SELECT {{0}}.*, Row_Number() over ({GetOrderByClause("{0}")}) as RowNum", false);
            return $"SELECT *\r\nFROM ({select}) RS\r\nWHERE RowNum >= @_minrow And RowNum <= @_maxrow\r\nORDER BY RowNum";

        }

        /// <summary>
        /// The pages are sorted by orderBy, but the results within each page are not.
        /// </summary>
        /// <param name="innerSql"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="parameters"></param>
        /// <param name="tableName"></param>
        /// <param name="orderBy"></param>
        /// <param name="innerColumnSelector"></param>
        /// <returns></returns>
        [Obsolete("Use the QueryBuilder as this method does not sort within the page returned")]
        public static string PaginateQuery(string innerSql, int skip, int take, CommandParameters parameters,
            string tableName, string orderBy, string innerColumnSelector = "[Id]")
        {
            parameters.Add("_minrow", skip + 1);
            parameters.Add("_maxrow", take + skip);
            return $@"SELECT *
FROM dbo.[{tableName}]
WHERE {innerColumnSelector} IN
(
 SELECT {innerColumnSelector}
 FROM 
 (
  SELECT RESULT.{innerColumnSelector}, Row_Number() over (ORDER BY {orderBy}) as RowNum
  FROM ({innerSql}) RESULT
 ) RS 
 WHERE RowNum >= @_minrow And RowNum <= @_maxrow
)";
        }

        public string DeleteQuery()
        {
            if (Joins.Any())
                throw new NotSupportedException("Joins are not supported in delete operations");
            return "DELETE " + GetClauses(false);
        }

        public void AddOrder(string fieldName, bool descending)
        {
            if (fieldName.EndsWith(" desc", StringComparison.OrdinalIgnoreCase))
            {
                fieldName = fieldName.Substring(0, fieldName.Length - 5);
                descending = true;
            }

            fieldName = fieldName.Trim('[', ']');
            fieldName = "[" + fieldName + "]";
            fieldName = descending ? fieldName + " DESC" : fieldName;
            if (!orderByFields.Contains(fieldName))
                orderByFields = orderByFields.Concat(new[] { fieldName }).ToArray();
        }

        public void AddWhere(WhereParameter whereParams)
        {
            OpenSubClause();
            AppendField(whereParams.FieldName);
            AppendOperand(whereParams.Operand);
            AppendParameter(whereParams.ParameterName);
            CloseSubClause();

            AddParameter(whereParams.ParameterName, whereParams.Value);
        }
        public void AddWhereIn(WhereParameter whereParams)
        {
            var values = ((IEnumerable) whereParams.Value).OfType<object>().Select(v => v.ToString()).ToArray();
            if (values.Any())
            {
                var parameterNames = Enumerable.Range(0, values.Length)
                    .Select(i => Normalise($"{whereParams.ParameterName}{i}"))
                    .ToArray();
                var inClause = string.Join(", ", parameterNames.Select(p => "@" + p));

                OpenSubClause();
                AppendField(whereParams.FieldName);
                AppendOperand(whereParams.Operand);
                AppendInParameter(inClause);
                CloseSubClause();

                for (var i = 0; i < values.Length; i++)
                {
                    AddParameter(parameterNames[i], values[i]);
                }
            }
            else
            {
                AppendFalseClause();
            }
        }

        // Only certain characters are allowed in SQL parameter names: https://msdn.microsoft.com/en-us/library/ms175874.aspx?f=255&mspperror=-2147217396#Anchor_1
        // but for now we will keep it simple (e.g by not using a generic regex here) 
        // to make sure we don't put any unnecessary load on our Server that is already struggling in certain scenarios.  
        // https://blogs.msdn.microsoft.com/debuggingtoolbox/2008/04/02/comparing-regex-replace-string-replace-and-stringbuilder-replace-which-has-better-performance/
        static string Normalise(string value)
        {
            return value
                .Replace('-', '_')
                .Replace(' ', '_')
                .ToLower();
        }

        public void AddWhere(WhereParameter whereParams, object startValue, object endValue)
        {
            OpenSubClause();
            AppendField(whereParams.FieldName);
            AppendOperand(whereParams.Operand);
            AppendParameter("StartValue");
            AndAlso();
            AppendParameter("EndValue");
            CloseSubClause();

            AddParameter("StartValue", startValue);
            AddParameter("EndValue", endValue);
        }

        public void AddWhere(string whereClause)
        {
            whereClause = Regex.Replace(whereClause, @"@\w+", m => m.Value.ToLower());
            OpenSubClause();
            queryText.Append(whereClause);
            CloseSubClause();
        }

        public void WhereEquals(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.Equal,
                Value = value
            });
        }

        public void WhereNotEquals(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.NotEqual,
                Value = value
            });
        }

        public void WhereIn(string fieldName, object values)
        {
            AddWhereIn(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.In,
                Value = values
            });
        }

        public void WhereStartsWith(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.StartsWith,
                Value = GetStartsWithValue(value)
            });
        }

        public void WhereEndsWith(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.EndsWith,
                Value = GetEndsWithValue(value)
            });
        }

        public void WhereBetween(string fieldName, object startValue, object endValue)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.Between
            },
            startValue,
            endValue);
        }

        public void WhereBetweenOrEqual(string fieldName, object startValue, object endValue)
        {
            OpenSubClause();
            AppendField(fieldName);
            AppendOperand(SqlOperand.GreaterThanOrEqual);
            AppendParameter("StartValue");
            AndAlso();
            AppendField(fieldName);
            AppendOperand(SqlOperand.LessThanOrEqual);
            AppendParameter("EndValue");
            CloseSubClause();

            AddParameter("StartValue", startValue);
            AddParameter("EndValue", endValue);
        }

        public void WhereGreaterThan(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.GreaterThan,
                Value = value
            });
        }

        public void WhereGreaterThanOrEqual(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.GreaterThanOrEqual,
                Value = value
            });
        }

        public void WhereLessThan(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.LessThan,
                Value = value
            });
        }

        public void WhereLessThanOrEqual(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.LessThanOrEqual,
                Value = value
            });
        }

        public void WhereContains(string fieldName, object value)
        {
            AddWhere(new WhereParameter
            {
                FieldName = fieldName,
                Operand = SqlOperand.Contains,
                Value = GetContainsValue(value)
            });
        }

        public void WhereContainsAny(string fieldName, IEnumerable<object> values)
        {
            throw new NotImplementedException();
        }

        public void WhereContainsAll(string fieldName, IEnumerable<object> values)
        {
            throw new NotImplementedException();
        }

        public void OpenSubClause()
        {
            AppendAndIfNeeded();
            queryText.Append("(");
        }

        public void CloseSubClause()
        {
            queryText.Append(")");
        }

        public void AndAlso()
        {
            queryText.Append(" AND ");
        }

        public void OrElse()
        {
            queryText.Append(" OR ");
        }

        public void AddParameter(string fieldName, object value)
        {
            queryParameters.Add(fieldName.ToLower(), value);
        }

        public void AddJoin(IJoin join)
        {
            Joins.Add(join);
        }


        void AppendField(string fieldName)
        {
            queryText.Append(Field(fieldName));
        }

        string Field(string fieldName)
        {
            return $"[{fieldName}]";
        }

        void AppendOperand(SqlOperand operand)
        {
            queryText.Append(Operand(operand));
        }

        string Operand(SqlOperand operand)
        {
            return $" {GetQueryOperand(operand)} ";
        }

        void AppendParameter(string fieldName)
        {
            queryText.Append(Parameter(fieldName));
        }

        void AppendInParameter(string inClause)
        {
            queryText.Append("(");
            queryText.Append(inClause);
            queryText.Append(")");
        }

        void AppendFalseClause()
        {
            queryText.Append("0 = 1");
        }

        string Parameter(string fieldName)
        {
            return $"@{fieldName.ToLower()}";
        }

        string GetContainsValue(object value)
        {
            return $"%{value}%";
        }

        string GetStartsWithValue(object value)
        {
            return $"{value}%";
        }

        string GetEndsWithValue(object value)
        {
            return $"%{value}";
        }

        string GetQueryOperand(SqlOperand operand)
        {
            switch (operand)
            {
                case SqlOperand.Between:
                    return "BETWEEN";
                case SqlOperand.Contains:
                case SqlOperand.ContainsAll:
                case SqlOperand.ContainsAny:
                case SqlOperand.EndsWith:
                case SqlOperand.StartsWith:
                    return "LIKE";
                case SqlOperand.Equal:
                    return "=";
                case SqlOperand.NotEqual:
                    return "<>";
                case SqlOperand.GreaterThan:
                    return ">";
                case SqlOperand.GreaterThanOrEqual:
                    return ">=";
                case SqlOperand.LessThan:
                    return "<";
                case SqlOperand.LessThanOrEqual:
                    return "<=";
                case SqlOperand.In:
                    return "IN";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }

        void AppendAndIfNeeded()
        {
            if (queryText.Length > 0)
                AndAlso();
        }

        string GetClauses(bool orderBy)
        {
            var sb = new StringBuilder();

            sb.Append("FROM dbo.[").Append(ViewOrTableName).Append("]");
            if (!string.IsNullOrEmpty(tableHint))
                sb.Append(" ").Append(tableHint);

            var whereClause = GetWhereClause();
            if (!string.IsNullOrEmpty(whereClause))
                sb.Append(" ").Append(whereClause);

            if (orderBy)
            {
                var orderByClause = GetOrderByClause();
                if (!string.IsNullOrEmpty(orderByClause))
                    sb.Append(" ").Append(orderByClause);
            }
            return sb.ToString();
        }

        string GetClausesForJoinQuery(string select, bool orderby)
        {
            var leftTableAlias = tableAliasGenerator.GenerateTableAlias(ViewOrTableName);
            var sb = new StringBuilder();
            sb.AppendFormat(select, leftTableAlias)
                .AppendLine($" FROM (SELECT * {GetClauses(false)}) {leftTableAlias}");

            foreach (var join in Joins)
            {
                var right = join.RightQuery;
                var rightQuery = right.SelectQuery(false);
                var rightTableAlias = tableAliasGenerator.GenerateTableAlias(right.ViewOrTableName);

                var joinClauseString = JoinClauseString(leftTableAlias, rightTableAlias, join.JoinClauses);
                var joinTypeString = JoinTypeString(join.JoinType);

                sb.AppendLine($"{joinTypeString} ({rightQuery}) {rightTableAlias} {joinClauseString}");
            }

            if (orderby)
            {
                var orderByClause = GetOrderByClause(leftTableAlias);
                if (!string.IsNullOrEmpty(orderByClause))
                    sb.AppendLine(orderByClause);
            }
            return sb.ToString().Trim();
        }

        string JoinClauseString(string leftTableAlias, string rightTableAlias, ICollection<JoinClause> joinClauses)
        {
            if (!joinClauses.Any())
                throw new InvalidOperationException("Can not create empty join clause");

            var joinClauseStrings = joinClauses.Select(j => j.ToString(leftTableAlias, rightTableAlias)).ToArray();
            var joinString = "ON " + string.Join(" AND ", joinClauseStrings);

            return joinString;
        }

        string JoinTypeString(JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    return "INNER JOIN";
                default:
                    throw new NotSupportedException($"Join {joinType} is not supported");
            }
        }
    }

    public enum SqlOperand
    {
        Equal,
        In,
        StartsWith,
        EndsWith,
        Between,
        BetweenOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        NotEqual,
        Contains,
        ContainsAny,
        ContainsAll
    }
}
