using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nevermore.Advanced.ReaderStrategies.Compilation;

namespace Nevermore.Advanced.ReaderStrategies.AnonymousTypes
{
    public class AnonymousTypeReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public AnonymousTypeReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public bool CanRead(Type type)
        {
            return type.IsGenericType &&
                   type.Namespace == null &&
                   type.IsSealed &&
                   type.BaseType == typeof(object) &&
                   !type.IsPublic &&
                   type.GetCustomAttribute<CompilerGeneratedAttribute>() != null &&
                   type.Name.Contains("AnonymousType");
        }

        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            return command =>
            {
                CompiledExpression<AnonymousTypeReaderFunc<TRecord>> compiled = null;
                var rowNumber = 0;
                var context = new AnonymousTypeReaderContext();

                return reader =>
                {
                    rowNumber++;

                    if (compiled == null)
                    {
                        compiled = Compile<TRecord>(reader);
                    }

                    try
                    {
                        var instance = compiled.Execute(reader, context);
                        return (instance, true);
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException(rowNumber, context.Column, compiled.ExpressionSource, ex);
                    }
                };
            };
        }

        CompiledExpression<AnonymousTypeReaderFunc<TRecord>> Compile<TRecord>(IDataRecord record)
        {
            var constructor = typeof(TRecord).GetConstructors().Single();
            var constructorParams = constructor.GetParameters();

            var readerArg = Expression.Parameter(typeof(DbDataReader), "reader");
            var contextArg = Expression.Parameter(typeof(AnonymousTypeReaderContext), "context");

            var locals = new List<ParameterExpression>();
            var body = new List<Expression>();

            var expectedFieldCount = record.FieldCount;
            for (var i = 0; i < expectedFieldCount; i++)
            {
                var param = constructorParams[i];
                var paramLocal = Expression.Variable(param.ParameterType, $"p{i}");
                locals.Add(paramLocal);
                body.Add(Expression.Assign(Expression.Field(contextArg, nameof(AnonymousTypeReaderContext.Column)), Expression.Constant(i)));
                body.Add(Expression.Assign(paramLocal, ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(i), param.ParameterType, configuration.TypeHandlers)));
            }

            body.Add(Expression.New(constructor, locals));

            var block = Expression.Block(locals, body);

            var lambda = Expression.Lambda<AnonymousTypeReaderFunc<TRecord>>(block, readerArg, contextArg);

            return ExpressionCompiler.Compile(lambda);
        }
    }
}