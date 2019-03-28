using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.AST
{
    public class Where
    {
        readonly IWhereClause whereClause; // Can be null

        public Where()
        {
        }

        public Where(IWhereClause whereClause)
        {
            this.whereClause = whereClause;
        }

        public string GenerateSql()
        {
            return whereClause != null ? $@"
WHERE {whereClause.GenerateSql()}" : string.Empty;
        }

        public override string ToString() => GenerateSql();
    }

    public interface IWhereClause
    {
        string GenerateSql();
    }

    public class AndClause : IWhereClause
    {
        readonly IReadOnlyList<IWhereClause> subClauses;

        public AndClause(IReadOnlyList<IWhereClause> subClauses)
        {
            this.subClauses = subClauses;
        }

        public string GenerateSql() => string.Join(@"
AND ", subClauses.Select(c => $"({c.GenerateSql()})"));
        public override string ToString() => GenerateSql();
    }

    public class ArrayWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly ArraySqlOperand operand;
        readonly IEnumerable<string> parameterNames;

        public ArrayWhereClause(IWhereFieldReference whereFieldReference, ArraySqlOperand operand, IEnumerable<string> parameterNames)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.parameterNames = parameterNames;
        }

        public string GenerateSql() => $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} ({string.Join(", ", parameterNames.Select(p => $"@{p}"))})";
        public override string ToString() => GenerateSql();

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case ArraySqlOperand.In:
                    return "IN";
                default:
                    throw new ArgumentOutOfRangeException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class BinaryWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly BinarySqlOperand operand;
        readonly string firstParameterName;
        readonly string secondParameterName;

        public BinaryWhereClause(IWhereFieldReference whereFieldReference, BinarySqlOperand operand, string firstParameterName, string secondParameterName)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.firstParameterName = firstParameterName;
            this.secondParameterName = secondParameterName;
        }

        public string GenerateSql() => $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} @{firstParameterName} AND @{secondParameterName}";
        public override string ToString() => GenerateSql();

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case BinarySqlOperand.Between:
                    return "BETWEEN";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class CustomWhereClause : IWhereClause
    {
        readonly string whereClause;

        public CustomWhereClause(string whereClause)
        {
            this.whereClause = whereClause;
        }

        public string GenerateSql() => whereClause;
        public override string ToString() => GenerateSql();
    }

    public class UnaryWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly UnarySqlOperand operand;
        readonly string parameterName;

        public UnaryWhereClause(IWhereFieldReference whereFieldReference, UnarySqlOperand operand, string parameterName)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.parameterName = parameterName;
        }

        public string GenerateSql() => $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} {parameterName}";
        public override string ToString() => GenerateSql();

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case UnarySqlOperand.Like:
                    return "LIKE";
                case UnarySqlOperand.Equal:
                    return "=";
                case UnarySqlOperand.NotEqual:
                    return "<>";
                case UnarySqlOperand.GreaterThan:
                    return ">";
                case UnarySqlOperand.GreaterThanOrEqual:
                    return ">=";
                case UnarySqlOperand.LessThan:
                    return "<";
                case UnarySqlOperand.LessThanOrEqual:
                    return "<=";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class IsNullClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly bool not;

        public IsNullClause(IWhereFieldReference whereFieldReference, bool not = false)
        {
            this.whereFieldReference = whereFieldReference;
            this.not = not;
        }

        string NotPart => not ? " not " : " ";

        public string GenerateSql() => $"{whereFieldReference.GenerateSql()} is{NotPart}null";
    }
}