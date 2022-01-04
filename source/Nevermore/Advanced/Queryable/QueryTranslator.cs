using System;
using System.Collections;
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
        ISelectColumns columnSelection;
        QueryType queryType = QueryType.SelectMany;
        int? skip;
        int? take;

        public QueryTranslator(IRelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
            documentMap = configuration.DocumentMaps.Resolve<TDocument>();
        }

        public (PreparedCommand, QueryType) Translate(Expression expression)
        {
            Visit(expression);

            string sql;
            var generateRowNumbers = skip.HasValue || take.HasValue;
            if (queryType == QueryType.Exists)
            {
                var select = new Select(
                    rowSelection ?? new AllRows(),
                    new SelectAllSource(),
                    from,
                    whereClauses.Any() ? new Where(new AndClause(whereClauses)) : new Where(),
                    null,
                    null);
                var trueParameter = AddParameter(true);
                var falseParameter = AddParameter(false);
                sql = new IfExpression(new ExistsExpression(select), new SelectConstant(trueParameter), new SelectConstant(falseParameter)).GenerateSql();
            }
            else
            {
                var orderBy = orderByClauses.Any() ? new OrderBy(orderByClauses) : new OrderBy(new[] { new OrderByField(new Column("Id")) });
                var columns = columnSelection ?? new SelectAllSource();
                var select = new Select(
                    rowSelection ?? new AllRows(),
                    generateRowNumbers ? new AggregateSelectColumns(new ISelectColumns[] { new SelectRowNumber(new Over(orderBy, null), "RowNum"), new SelectAllSource() }) : columns,
                    from,
                    whereClauses.Any() ? new Where(new AndClause(whereClauses)) : new Where(),
                    null,
                    orderByClauses.Any() && !generateRowNumbers ? orderBy : null);

                if (generateRowNumbers)
                {
                    var pagingFilters = new List<IWhereClause>();
                    if (skip.HasValue)
                    {
                        var skipParam = AddParameter(skip.Value);
                        pagingFilters.Add(new UnaryWhereClause(new WhereFieldReference("RowNum"), UnarySqlOperand.GreaterThan, skipParam.ParameterName));
                    }

                    if (take.HasValue)
                    {
                        var takeParam = AddParameter(take.Value - (skip ?? 0));
                        pagingFilters.Add(new UnaryWhereClause(new WhereFieldReference("RowNum"), UnarySqlOperand.LessThanOrEqual, takeParam.ParameterName));
                    }

                    select = new Select(
                        new AllRows(),
                        new SelectAllFrom("aliased"),
                        new SubquerySource(select, "aliased"),
                        new Where(new AndClause(pagingFilters)),
                        null,
                        new OrderBy(new[] { new OrderByField(new Column("RowNum")) }));
                }

                sql = select.GenerateSql();
            }
            return (new PreparedCommand(sql, parameterValues, RetriableOperation.Select), queryType);
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
                queryType = QueryType.SelectSingle;
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.FirstOrDefault))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    AddWhere(expression.Body);
                }

                rowSelection = new Top(1);
                queryType = QueryType.SelectSingle;
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Any))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    AddWhere(expression.Body);
                }

                queryType = QueryType.Exists;
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Count))
            {
                Visit(node.Arguments[0]);

                if (node.Arguments.Count > 1)
                {
                    var expression = (LambdaExpression)StripQuotes(node.Arguments[1]);
                    AddWhere(expression.Body);
                }

                columnSelection = new SelectCountSource();
                queryType = QueryType.Count;
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Take))
            {
                Visit(node.Arguments[0]);
                take = (int)GetValueFromExpression(node.Arguments[1], typeof(int));
                return node;
            }

            if (methodInfo.Name == nameof(System.Linq.Queryable.Skip))
            {
                Visit(node.Arguments[0]);
                skip = (int)GetValueFromExpression(node.Arguments[1], typeof(int));
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
            if (expression.Arguments.Count == 1 && expression.Method.DeclaringType == typeof(string))
            {
                AddStringMethodWhere(expression);
            }

            if (expression.Method.Name == "Contains")
            {
                var (fieldReference, _) = GetFieldReferenceAndType(expression.Arguments.Count == 1 ? expression.Arguments[0] : expression.Arguments[1]);
                var values = (IEnumerable)GetValueFromExpression(expression.Arguments.Count == 1 ? expression.Object : expression.Arguments[0], typeof(IEnumerable));
                var parameters = (from object x in values select AddParameter(x)).ToList();

                whereClauses.Add(new ArrayWhereClause(fieldReference, ArraySqlOperand.In, parameters.Select(p => p.ParameterName)));
            }
        }

        void AddStringMethodWhere(MethodCallExpression expression)
        {
            var (fieldReference, _) = GetFieldReferenceAndType(expression.Object);
            var value = (string)GetValueFromExpression(expression.Arguments[0], typeof(string));

            if (expression.Method.Name == nameof(string.Contains))
            {
                var parameter = AddParameter($"%{value}%");
                whereClauses.Add(new UnaryWhereClause(fieldReference, UnarySqlOperand.Like, parameter.ParameterName));
            }
            else if (expression.Method.Name == nameof(string.StartsWith))
            {
                var parameter = AddParameter($"{value}%");
                whereClauses.Add(new UnaryWhereClause(fieldReference, UnarySqlOperand.Like, parameter.ParameterName));
            }
            else if (expression.Method.Name == nameof(string.EndsWith))
            {
                var parameter = AddParameter($"%{value}");
                whereClauses.Add(new UnaryWhereClause(fieldReference, UnarySqlOperand.Like, parameter.ParameterName));
            }
        }

        void AddBinaryWhere(BinaryExpression expression)
        {
            if (expression.NodeType == ExpressionType.AndAlso)
            {
                AddWhere(expression.Left);
                AddWhere(expression.Right);
                return;
            }

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