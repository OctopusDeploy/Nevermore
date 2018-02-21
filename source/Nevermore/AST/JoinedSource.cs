using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.AST
{
    public class JoinedSource : ISelectSource
    {
        public Join LastJoin { get; }
        public IAliasedSelectSource Source { get; }
        public IReadOnlyList<Join> IntermediateJoins { get; }

        public JoinedSource(IAliasedSelectSource source, IReadOnlyList<Join> intermediateJoins, Join lastJoin) // todo, don't need intermediate + last joins here
        {
            this.LastJoin = lastJoin;
            IntermediateJoins = intermediateJoins;
            Source = source;
        }

        public JoinedSource AddClause(JoinClause clause)
        {
            return new JoinedSource(Source, IntermediateJoins, new Join(LastJoin.Clauses.Concat(new [] {clause}).ToList(), LastJoin.Source, LastJoin.Type));
        }

        public string GenerateSql()
        {
            var sourceParts = new [] {Source.GenerateSql()}.Concat(IntermediateJoins.Concat(new [] {LastJoin}).Select(j => j.GenerateSql(Source.Alias)));
            return string.Join("\r\n", sourceParts);
        }
    }

    public class Join
    {
        public Join(IReadOnlyList<JoinClause> clauses, IAliasedSelectSource source, JoinType type)
        {
            Clauses = clauses;
            Source = source;
            Type = type;
        }

        public IAliasedSelectSource Source { get; }
        public JoinType Type { get; }
        public IReadOnlyList<JoinClause> Clauses { get; }

        public string GenerateSql(string leftSourceAlias)
        {
            return $"{GenerateJoinTypeSql(Type)} {Source.GenerateSql()} ON {string.Join(" AND ", Clauses.Select(c => c.GenerateSql(leftSourceAlias, Source.Alias)))}";
        }

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