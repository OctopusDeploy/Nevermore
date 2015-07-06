using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nevermore
{
    public class SqlQueryGenerator : IQueryGenerator
    {
        readonly StringBuilder queryText = new StringBuilder();
        readonly CommandParameters queryParameters = new CommandParameters();

        const string defaultOrderBy = "Id";
        string[] orderByFields = new string[0];

        string viewOrTableName;
        string storedProcedureName;
        string tableHint;

        public SqlQueryGenerator(string viewOrTableName)
        {
            this.viewOrTableName = viewOrTableName;
        }

        public CommandParameters QueryParameters
        {
            get { return queryParameters; }
        }

        string GetOrderByClause()
        {
            return orderByFields.Any()
                ? string.Join(", ", orderByFields)
                : defaultOrderBy;
        }

        string GetWhereClause()
        {
            return queryText.Length > 0
                ? "WHERE " + queryText
                : string.Empty;
        }

        public override string ToString()
        {
            return (tableHint == null ? "" : tableHint + " ") + GetWhereClause() + " ORDER BY " + GetOrderByClause();
        }

        public void UseTable(string tableName)
        {
            viewOrTableName = tableName;
        }

        public void UseView(string viewName)
        {
            viewOrTableName = viewName;
        }

        public void UseStoredProcedure(string storedProcedureName)
        {
            this.storedProcedureName = storedProcedureName;
        }

        public void UseHint(string hintClause)
        {
            tableHint = hintClause;
        }

        public string CountQuery()
        {
            return "SELECT COUNT(*) FROM dbo.[" + viewOrTableName + "] " + tableHint + " " + GetWhereClause();
        }

        public string TopQuery(int top = 1)
        {
            return "SELECT TOP " + top + " * FROM dbo.[" + viewOrTableName + "] " + ToString();
        }

        public string SelectQuery()
        {
            return "SELECT * FROM dbo.[" + viewOrTableName + "] " + ToString();
        }

        public string PaginateQuery(int skip, int take)
        {
            AddParameter("_minrow", skip + 1);
            AddParameter("_maxrow", take + skip);
            return "SELECT * FROM (SELECT *, Row_Number() over (ORDER BY " + GetOrderByClause() + ") as RowNum FROM dbo.[" + viewOrTableName + "] " + GetWhereClause() + ") RS WHERE RowNum >= @_minrow And RowNum <= @_maxrow";
        }

        public void AddOrder(string fieldName, bool descending)
        {
            fieldName = "[" + fieldName + "]";
            fieldName = descending ? fieldName + " DESC" : fieldName;
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

        public void WhereIn(string fieldName, object values)
        {
            AddWhere(new WhereParameter
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
            if (value is IEnumerable<object>)
                value = GetInValue(value);

            queryParameters.Add(fieldName.ToLower(), value);
        }

        void AppendField(string fieldName)
        {
            queryText.Append(Field(fieldName));
        }

        string Field(string fieldName)
        {
            return string.Format("[{0}]", fieldName);
        }

        void AppendOperand(SqlOperand operand)
        {
            queryText.Append(Operand(operand));
        }

        string Operand(SqlOperand operand)
        {
            return string.Format(" {0} ", GetQueryOperand(operand));
        }

        void AppendParameter(string fieldName)
        {
            queryText.Append(Parameter(fieldName));
        }

        string Parameter(string fieldName)
        {
            return string.Format("@{0}", fieldName.ToLower());
        }

        string GetContainsValue(object value)
        {
            return string.Format("%{0}%", value);
        }

        string GetStartsWithValue(object value)
        {
            return string.Format("{0}%", value);
        }

        string GetEndsWithValue(object value)
        {
            return string.Format("%{0}", value);
        }

        string GetInValue(object values)
        {
            var inVals = new StringBuilder();
            inVals.Append("(");
            inVals.Append(string.Join(", ", (values as IEnumerable<object>)));
            inVals.Append(")");
            return inVals.ToString();
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
        Contains,
        ContainsAny,
        ContainsAll
    }
}
