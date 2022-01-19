using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Querying.AST;
using Nevermore.Util;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryTranslator : ExpressionVisitor
    {
        readonly SqlExpressionBuilder sqlBuilder;

        public QueryTranslator(IRelationalStoreConfiguration configuration)
        {
            sqlBuilder = new SqlExpressionBuilder(configuration);
        }

        public (PreparedCommand, QueryType) Translate(Expression expression)
        {
            Visit(expression);

            var (sqlExpression, parameterValues, queryType) = sqlBuilder.Build();
            return (new PreparedCommand(sqlExpression.GenerateSql(), parameterValues, RetriableOperation.Select), queryType);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q)
            {
                sqlBuilder.From(q.ElementType);
                return node;
            }

            throw new NotSupportedException();
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodInfo = node.Method;
            if (methodInfo.DeclaringType != typeof(System.Linq.Queryable) && methodInfo.DeclaringType != typeof(NevermoreQueryableExtensions))
            {
                throw new NotSupportedException();
            }

            if (methodInfo.Name == nameof(NevermoreQueryableExtensions.WhereCustom))
            {
                Visit(node.Arguments[0]);
                if (node.Arguments[1] is ConstantExpression { Value: string } constantExpression)
                {
                    sqlBuilder.Where(new CustomWhereClause((string)constantExpression.Value));
                    return node;
                }
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Where))
            {
                Visit(node.Arguments[0]);
                var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                sqlBuilder.Where(CreateWhereClause(expression.Body));
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.OrderBy))
            {
                if (node.Arguments.Count > 2)
                {
                    throw new NotSupportedException("OrderBy does not support custom comparers");
                }

                Visit(node.Arguments[0]);
                var fieldName = GetMemberNameFromKeySelectorExpression(node.Arguments[1]);
                sqlBuilder.OrderBy(new OrderByField(new Column(fieldName)));
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.OrderByDescending))
            {
                if (node.Arguments.Count > 2)
                {
                    throw new NotSupportedException("OrderBy does not support custom comparers");
                }

                Visit(node.Arguments[0]);
                var fieldName = GetMemberNameFromKeySelectorExpression(node.Arguments[1]);
                sqlBuilder.OrderBy(new OrderByField(new Column(fieldName), OrderByDirection.Descending));
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.First))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    sqlBuilder.Where(CreateWhereClause(expression.Body));
                }

                sqlBuilder.Single();
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.FirstOrDefault))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    sqlBuilder.Where(CreateWhereClause(expression.Body));
                }

                sqlBuilder.Single();
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Any))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    sqlBuilder.Where(CreateWhereClause(expression.Body));
                }

                sqlBuilder.Exists();
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Count))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    sqlBuilder.Where(CreateWhereClause(expression.Body));
                }

                sqlBuilder.Count();
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Take))
            {
                Visit(node.Arguments[0]);
                var take = (int)GetValueFromExpression(node.Arguments[1], typeof(int));
                sqlBuilder.Take(take);
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Skip))
            {
                Visit(node.Arguments[0]);
                var skip = (int)GetValueFromExpression(node.Arguments[1], typeof(int));
                sqlBuilder.Skip(skip);
                return node;
            }

            throw new NotSupportedException();
        }

        static string GetMemberNameFromKeySelectorExpression(Expression expression)
        {
            var expressionWithoutQuotes = StripQuotes(expression);

            if (expressionWithoutQuotes is LambdaExpression { Body: MemberExpression { NodeType: ExpressionType.MemberAccess } memberExpression })
            {
                return memberExpression.Member.Name;
            }

            throw new NotSupportedException();
        }

        IWhereClause CreateWhereClause(Expression expression, bool invert = false)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                return CreateBinaryWhere(binaryExpression);
            }
            if (expression is MethodCallExpression methodCallExpression)
            {
                return CreateMethodCallWhere(methodCallExpression, invert);
            }

            if (expression is UnaryExpression { NodeType: ExpressionType.Not } unaryExpression)
            {
                return CreateWhereClause(unaryExpression.Operand, true);
            }

            if (expression is MemberExpression { Member: { MemberType: MemberTypes.Property } and PropertyInfo propertyInfo } && propertyInfo.PropertyType == typeof(bool))
            {
                var (fieldReference, _) = GetFieldReferenceAndType(expression);
                return sqlBuilder.CreateWhere(fieldReference, UnarySqlOperand.Equal, !invert);
            }

            throw new NotSupportedException();
        }

        IWhereClause CreateMethodCallWhere(MethodCallExpression expression, bool invert = false)
        {
            if (expression.Arguments.Count == 1 && expression.Method.DeclaringType == typeof(string))
            {
                return CreateStringMethodWhere(expression, invert);
            }

            if (expression.Method.Name == "Contains")
            {
                var left = expression.Arguments.Count == 1 ? expression.Object : expression.Arguments[0];
                var right = expression.Arguments.Count == 1 ? expression.Arguments[0] : expression.Arguments[1];

                if (left is MemberExpression { Member: PropertyInfo propertyInfo } memberExpressionL && memberExpressionL.IsBasedOff<ParameterExpression>())
                {
                    var (fieldReference, type) = GetFieldReferenceAndType(left);
                    var value = GetValueFromExpression(right, type);

                    if (fieldReference is WhereFieldReference)
                    {
                        return sqlBuilder.CreateWhere(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, $"%|{value}|%");
                    }

                    if (fieldReference is JsonValueFieldReference or JsonQueryFieldReference)
                    {
                        var jsonPath = GetJsonPath(memberExpressionL);
                        var elementType = propertyInfo.PropertyType.GetSequenceType();
                        return sqlBuilder.CreateWhere(value, invert ? ArraySqlOperand.NotIn : ArraySqlOperand.In, jsonPath, elementType);
                    }
                }
                else if (right is MemberExpression memberExpressionR && memberExpressionR.IsBasedOff<ParameterExpression>())
                {
                    var values = (IEnumerable)GetValueFromExpression(left, typeof(IEnumerable));
                    var (fieldReference, _) = GetFieldReferenceAndType(right);
                    return sqlBuilder.CreateWhere(fieldReference, invert ? ArraySqlOperand.NotIn : ArraySqlOperand.In, values);
                }
            }

            throw new NotSupportedException();
        }

        IWhereClause CreateStringMethodWhere(MethodCallExpression expression, bool invert = false)
        {
            var (fieldReference, _) = GetFieldReferenceAndType(expression.Object);
            var value = (string)GetValueFromExpression(expression.Arguments[0], typeof(string));

            if (expression.Method.Name == nameof(string.Contains))
            {
                return sqlBuilder.CreateWhere(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, $"%{value}%");
            }
            if (expression.Method.Name == nameof(string.StartsWith))
            {
                return sqlBuilder.CreateWhere(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, $"{value}%");
            }
            if (expression.Method.Name == nameof(string.EndsWith))
            {
                return sqlBuilder.CreateWhere(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, $"%{value}");
            }

            throw new NotSupportedException();
        }

        IWhereClause CreateBinaryWhere(BinaryExpression expression, bool invert = false)
        {
            if (expression.NodeType == ExpressionType.AndAlso)
            {
                var leftClause = CreateWhereClause(expression.Left);
                var rightClause = CreateWhereClause(expression.Right);
                return new AndClause(new[] { leftClause, rightClause });
            }

            if (expression.NodeType == ExpressionType.OrElse)
            {
                var leftClause = CreateWhereClause(expression.Left);
                var rightClause = CreateWhereClause(expression.Right);
                return new OrClause(new[] { leftClause, rightClause });
            }

            var op = expression.NodeType switch
            {
                ExpressionType.Equal => invert ? UnarySqlOperand.NotEqual : UnarySqlOperand.Equal,
                ExpressionType.NotEqual => invert ? UnarySqlOperand.Equal : UnarySqlOperand.NotEqual,
                ExpressionType.LessThan => invert ? UnarySqlOperand.GreaterThanOrEqual : UnarySqlOperand.LessThan,
                ExpressionType.LessThanOrEqual => invert ? UnarySqlOperand.GreaterThan : UnarySqlOperand.LessThanOrEqual,
                ExpressionType.GreaterThan => invert ? UnarySqlOperand.LessThanOrEqual : UnarySqlOperand.GreaterThan,
                ExpressionType.GreaterThanOrEqual => invert ? UnarySqlOperand.LessThan : UnarySqlOperand.GreaterThanOrEqual,
                _ => throw new NotSupportedException()
            };

            var (fieldReference, propertyType) = GetFieldReferenceAndType(expression.Left);
            var value = GetValueFromExpression(expression.Right, propertyType);

            if (value == null && op is UnarySqlOperand.Equal or UnarySqlOperand.NotEqual)
            {
                return new IsNullClause(fieldReference, op == UnarySqlOperand.NotEqual);
            }

            return sqlBuilder.CreateWhere(fieldReference, op, value);
        }

        object GetValueFromExpression(Expression expression, Type propertyType)
        {
            object result;
            if (expression is ConstantExpression constantExpression)
            {
                result = constantExpression.Value;
            }
            else
            {
                var objectMember = Expression.Convert(expression, typeof(object));
                var getterLambda = Expression.Lambda<Func<object>>(objectMember);
                result = getterLambda.Compile()();
            }

            if (propertyType.GetTypeInfo().IsEnum &&
                result != null &&
                result.GetType().GetTypeInfo().IsPrimitive)
                return Enum.ToObject(propertyType, result);

            return result;
        }

        (IWhereFieldReference, Type) GetFieldReferenceAndType(Expression expression)
        {
            if (expression is UnaryExpression unaryExpression)
                expression = unaryExpression.Operand;

            if (expression is MemberExpression { Member: PropertyInfo propertyInfo } memberExpression)
            {
                var documentMap = sqlBuilder.DocumentMap;
                var parameterExpression = memberExpression.FindChildOfType<ParameterExpression>();
                var childPropertyExpression = memberExpression.FindChildOfType<MemberExpression>();
                if (childPropertyExpression == null && parameterExpression.Type == documentMap.Type)
                {
                    if (documentMap.IdColumn!.Property.Matches(propertyInfo))
                    {
                        return (new WhereFieldReference(documentMap.IdColumn.ColumnName), documentMap.IdColumn.Type);
                    }

                    var column = documentMap.Columns.Where(c => c.Property is not null).FirstOrDefault(c => c.Property.Matches(propertyInfo));
                    if (column is not null)
                    {
                        return (new WhereFieldReference(column.ColumnName), propertyInfo.PropertyType);
                    }
                }

                if (documentMap.HasJsonColumn())
                {
                    var jsonPath = GetJsonPath(memberExpression);
                    IWhereFieldReference fieldReference = propertyInfo.IsScalar()
                        ? new JsonValueFieldReference(jsonPath)
                        : new JsonQueryFieldReference(jsonPath);
                    return (fieldReference, propertyInfo.PropertyType);
                }
            }

            throw new NotSupportedException();
        }

        static string GetJsonPath(MemberExpression memberExpression)
        {
            var segments = new List<string>();
            do
            {
                segments.Add(memberExpression.Member.Name);
                memberExpression = memberExpression.Expression as MemberExpression;
            } while (memberExpression is not null);
            segments.Reverse();
            return "$." + string.Join(".", segments);
        }

        static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;

            return expression;
        }
    }
}