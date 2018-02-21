using System;
using System.Collections.Generic;
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
            var sourceParts = new [] {Source.GenerateSql()}.Concat(joins.Select(j => j.GenerateSql(Source.Alias)));
            return string.Join("\r\n", sourceParts);
        }
    }

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

        public string GenerateSql(string leftSourceAlias) => 
            $"{GenerateJoinTypeSql(type)} {source.GenerateSql()} ON {string.Join(" AND ", clauses.Select(c => c.GenerateSql(leftSourceAlias, source.Alias)))}";

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

    public class JoinClause
    {
        readonly string leftFieldName;
        readonly JoinOperand operand;
        readonly string rightFieldName;

        public JoinClause(string leftFieldName, JoinOperand operand, string rightFieldName)
        {
            this.leftFieldName = leftFieldName;
            this.operand = operand;
            this.rightFieldName = rightFieldName;
        }

        public string GenerateSql(string leftTableAlias, string rightTableAlias)
        {
            return $"{leftTableAlias}.[{leftFieldName}] {GetQueryOperand()} {rightTableAlias}.[{rightFieldName}]";
        }

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