using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Nevermore.Mapping;
using Nevermore.Querying.AST;
using Nevermore.Util;

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
                var orderBy = orderByClauses.Any() ? new OrderBy(orderByClauses) : GetDefaultOrderBy();
                var columns = columnSelection ?? new SelectAllJsonColumnLast(GetDocumentColumns().ToList());
                var select = new Select(
                    rowSelection ?? new AllRows(),
                    generateRowNumbers ? new AggregateSelectColumns(new[] { new SelectRowNumber(new Over(orderBy, null), "RowNum"), columns }) : columns,
                    from,
                    whereClauses.Any() ? new Where(new AndClause(whereClauses)) : new Where(),
                    null,
                    orderByClauses.Any() && !generateRowNumbers && columnSelection is not SelectCountSource ? orderBy : null);

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
                        var takeParam = AddParameter(take.Value + (skip ?? 0));
                        pagingFilters.Add(new UnaryWhereClause(new WhereFieldReference("RowNum"), UnarySqlOperand.LessThanOrEqual, takeParam.ParameterName));
                    }

                    select = new Select(
                        new AllRows(),
                        new SelectAllColumnsWithTableAliasJsonLast("aliased", GetDocumentColumns().ToList()),
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
            if (methodInfo.DeclaringType != typeof(System.Linq.Queryable) && methodInfo.DeclaringType != typeof(NevermoreQueryableExtensions))
            {
                throw new NotSupportedException();
            }

            if (methodInfo.Name == nameof(NevermoreQueryableExtensions.WhereCustom))
            {
                Visit(node.Arguments[0]);
                if (node.Arguments[1] is ConstantExpression { Value: string } constantExpression)
                {
                    whereClauses.Add(new CustomWhereClause((string)constantExpression.Value));
                    return node;
                }
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
            whereClauses.Add(CreateWhereClause(expression));
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
                var (fieldReference, type) = GetFieldReferenceAndType(expression);
                var param = AddParameter(!invert);
                return new UnaryWhereClause(fieldReference, UnarySqlOperand.Equal, param.ParameterName);
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

                if (left is MemberExpression memberExpressionL && memberExpressionL.IsBasedOff<ParameterExpression>())
                {
                    var (fieldReference, type) = GetFieldReferenceAndType(left);
                    var value = GetValueFromExpression(right, type);

                    if (fieldReference is WhereFieldReference)
                    {
                        var param = AddParameter($"%|{value}|%");
                        return new UnaryWhereClause(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, param.ParameterName);
                    }

                    if (fieldReference is JsonValueFieldReference)
                    {
                        var param = AddParameter(value);
                        var op = invert ? "NOT IN" : "IN";
                        var jsonPath = GetJsonPath(memberExpressionL);
                        var dbType = value switch
                        {
                            string => "nvarchar(max)",
                            int => "int",
                            double => "double",
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        return new CustomWhereClause($"@{param.ParameterName} {op} (SELECT [Val] FROM OPENJSON([JSON], '{jsonPath}') WITH ([Val] {dbType} '$'))");
                    }
                }
                else if (right is MemberExpression memberExpressionR && memberExpressionR.IsBasedOff<ParameterExpression>())
                {
                    var values = (IEnumerable)GetValueFromExpression(left, typeof(IEnumerable));
                    var (fieldReference, _) = GetFieldReferenceAndType(right);
                    var parameters = (from object x in values select AddParameter(x)).ToList();
                    return parameters.Count == 0
                        ? new CustomWhereClause("1 = 0")
                        : new ArrayWhereClause(fieldReference, invert ? ArraySqlOperand.NotIn : ArraySqlOperand.In, parameters.Select(p => p.ParameterName));
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
                var parameter = AddParameter($"%{value}%");
                return new UnaryWhereClause(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, parameter.ParameterName);
            }
            if (expression.Method.Name == nameof(string.StartsWith))
            {
                var parameter = AddParameter($"{value}%");
                return new UnaryWhereClause(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, parameter.ParameterName);
            }
            if (expression.Method.Name == nameof(string.EndsWith))
            {
                var parameter = AddParameter($"%{value}");
                return new UnaryWhereClause(fieldReference, invert ? UnarySqlOperand.NotLike : UnarySqlOperand.Like, parameter.ParameterName);
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

            var parameter = AddParameter(value);
            return new UnaryWhereClause(fieldReference, op, parameter.ParameterName);
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

            if (expression is MemberExpression { Member: PropertyInfo propertyInfo } memberExpression)
            {
                var parameterExpression = memberExpression.FindChildOfType<ParameterExpression>();
                var childPropertyExpression = memberExpression.FindChildOfType<MemberExpression>();
                if (childPropertyExpression == null && parameterExpression.Type == typeof(TDocument))
                {
                    if (documentMap.IdColumn!.Property.Matches(propertyInfo))
                    {
                        return (new WhereFieldReference(documentMap.IdColumn.ColumnName), documentMap.IdColumn.Type);
                    }

                    var column = documentMap.Columns.Where(p => p is not null).FirstOrDefault(c => c.Property.Matches(propertyInfo));
                    if (column is not null)
                    {
                        return (new WhereFieldReference(column.ColumnName), propertyInfo.PropertyType);
                    }
                }

                if (documentMap.HasJsonColumn())
                {
                    var jsonPath = GetJsonPath(memberExpression);
                    return (new JsonValueFieldReference(jsonPath), propertyInfo.PropertyType);
                }
            }

            throw new NotSupportedException();
        }

        string GetJsonPath(MemberExpression memberExpression)
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

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable q && q.ElementType == typeof(TDocument))
            {
                var schema = documentMap.SchemaName ?? configuration.DefaultSchema;
                from = new SimpleTableSource(documentMap.TableName, schema, GetDocumentColumns().ToArray());
            }
            else
            {
                throw new Exception();
            }

            return node;
        }

        IEnumerable<string> GetDocumentColumns()
        {
            yield return documentMap.IdColumn!.ColumnName;

            foreach (var column in documentMap.Columns)
            {
                yield return column.ColumnName;
            }

            if (documentMap.HasJsonColumn())
            {
                yield return "JSON";
            }

            if (documentMap.HasJsonBlobColumn())
            {
                yield return "JSONBlob";
            }
        }

        OrderBy GetDefaultOrderBy()
        {
            return new OrderBy(new[] { new OrderByField(new Column(documentMap.IdColumn!.ColumnName)) });
        }
    }
}