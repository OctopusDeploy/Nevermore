using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nevermore.AST
{
    public class JoinedSource : ISelectSource
    {
        readonly IReadOnlyList<Join> joins;
        public IAliasedSelectSource Source { get; }

        public JoinedSource(IAliasedSelectSource source, IReadOnlyList<Join> joins)
        {
            this.joins = joins;
            Source = source;
        }

        public string GenerateSql()
        {
            var sourceParts = new [] {Source.GenerateSql()}.Concat(joins.Select(j => j.GenerateSql()));
            return string.Join("\r\n", sourceParts);
        }
        
        public override string ToString() => GenerateSql();
    }

    [DebuggerDisplay("{" + nameof(DebugSql) + "()}")]
    public class Join
    {
        readonly IAliasedSelectSource source;
        readonly JoinType type;
        readonly IReadOnlyList<JoinClause> clauses;

        public Join(IReadOnlyList<JoinClause> clauses, IAliasedSelectSource source, JoinType type)
        {
            this.clauses = clauses;
            this.source = source;
            this.type = type;
        }

        public string GenerateSql()
        {
            var separator = @"
AND ";
            return $@"{GenerateJoinTypeSql(type)} {source.GenerateSql()}
ON {string.Join(separator, clauses.Select(c => c.GenerateSql()))}";
        }

        string DebugSql() => GenerateSql();

        string GenerateJoinTypeSql(JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    return "INNER JOIN";
                case JoinType.LeftHashJoin:
                    return "LEFT HASH JOIN";
                default:
                    throw new NotSupportedException($"Join {joinType} is not supported");
            }
        }
    }

    public enum JoinType
    {
        InnerJoin,
        LeftHashJoin
    }

    public enum JoinOperand
    {
        Equal
    }

    [DebuggerDisplay("{" + nameof(DebugSql) + "()}")]
    public class JoinClause
    {
        readonly string leftTableAlias;
        readonly string leftFieldName;
        readonly JoinOperand operand;
        readonly string rightTableAlias;
        readonly string rightFieldName;

        public JoinClause(string leftTableAlias, string leftFieldName, JoinOperand operand, string rightTableAlias, string rightFieldName)
        {
            this.leftTableAlias = leftTableAlias;
            this.leftFieldName = leftFieldName;
            this.operand = operand;
            this.rightTableAlias = rightTableAlias;
            this.rightFieldName = rightFieldName;
        }

        public string GenerateSql()
        {
            return $"{leftTableAlias}.[{leftFieldName}] {GetQueryOperand()} {rightTableAlias}.[{rightFieldName}]";
        }

        string DebugSql() => GenerateSql();

        string GetQueryOperand()
        {
            switch (operand)
            {
                case JoinOperand.Equal:
                    return "=";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }
}