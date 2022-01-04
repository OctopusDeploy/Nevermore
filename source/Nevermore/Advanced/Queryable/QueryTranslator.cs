using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Nevermore.Mapping;
using Nevermore.Querying.AST;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryTranslator<TDocument> : ExpressionVisitor
    {
        readonly IRelationalStoreConfiguration configuration;
        readonly DocumentMap documentMap;
        readonly CommandParameterValues parameterValues = new();
        readonly List<OrderByField> orderByClauses = new();
        readonly List<IWhereClause> whereClauses = new();

        volatile int paramCounter;
        ISelectSource from;
        IRowSelection rowSelection;
        bool singleResult;

        public QueryTranslator(IRelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
            documentMap = configuration.DocumentMaps.Resolve<TDocument>();
        }

        public (PreparedCommand, bool) Translate(Expression expression)
        {
            Visit(expression);

            var select = new Select(
                rowSelection ?? new AllRows(),
                new SelectAllSource(),
                from,
                whereClauses.Any() ? new Where(new AndClause(whereClauses)) : new Where(),
                null,
                orderByClauses.Any() ? new OrderBy(orderByClauses) : null);
            var sql = select.GenerateSql();
            return (new PreparedCommand(sql, parameterValues, RetriableOperation.Select), singleResult);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var methodInfo = node.Method;
            if (methodInfo.DeclaringType != typeof(System.Linq.Queryable))
            {
                throw new NotSupportedException();
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Where))
            {
                Visit(node.Arguments[0]);
                var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                AddWhere(expression.Body);
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
                orderByClauses.Add(new OrderByField(new Column(fieldName)));
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
                orderByClauses.Add(new OrderByField(new Column(fieldName), OrderByDirection.Descending));
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.First))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    AddWhere(expression.Body);
                }

                rowSelection = new Top(1);
                singleResult = true;
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

        void AddWhere(Expression expression)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                AddBinaryWhere(binaryExpression);
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                AddMethodCallWhere(methodCallExpression);
            }
        }

        void AddMethodCallWhere(MethodCallExpression expression)
        {
            throw new NotImplementedException();
        }

        void AddBinaryWhere(BinaryExpression expression)
        {
            var op = expression.NodeType switch
            {
                ExpressionType.Equal => UnarySqlOperand.Equal,
                ExpressionType.NotEqual => UnarySqlOperand.NotEqual,
                ExpressionType.LessThan => UnarySqlOperand.LessThan,
                ExpressionType.LessThanOrEqual => UnarySqlOperand.LessThanOrEqual,
                ExpressionType.GreaterThan => UnarySqlOperand.GreaterThan,
                ExpressionType.GreaterThanOrEqual => UnarySqlOperand.GreaterThanOrEqual,
                _ => throw new NotSupportedException()
            };

            var (fieldReference, propertyType) = GetFieldReferenceAndType(expression.Left);
            var value = GetValueFromExpression(expression.Right, propertyType);

            if (value == null && op is UnarySqlOperand.Equal or UnarySqlOperand.NotEqual)
            {
                whereClauses.Add(new IsNullClause(fieldReference, op == UnarySqlOperand.NotEqual));
            }
            else
            {
                var parameter = AddParameter(value);
                whereClauses.Add(new UnaryWhereClause(fieldReference, op, parameter.ParameterName));
            }
        }

        Parameter AddParameter(object value)
        {
            var index = Interlocked.Increment(ref paramCounter);
            var paramName = $"p{index}";
            parameterValues[paramName] = value;
            return new Parameter(paramName);
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

            if (expression is MemberExpression { Member: PropertyInfo propertyInfo })
            {
                return (new WhereFieldReference(propertyInfo.Name), propertyInfo.PropertyType);
            }

            throw new NotSupportedException();
        }

        static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
                expression = ((UnaryExpression)expression).Operand;

            return expression;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q && q.ElementType == typeof(TDocument))
            {
                var schema = documentMap.SchemaName ?? configuration.DefaultSchema;
                from = new SimpleTableSource(documentMap.TableName, schema, new[] { "*" });
            }
            else
            {
                throw new Exception();
            }

            return node;
        }
    }
}