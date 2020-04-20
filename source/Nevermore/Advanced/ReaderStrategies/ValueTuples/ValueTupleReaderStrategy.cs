using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Nevermore.Advanced.ReaderStrategies.Compilation;

namespace Nevermore.Advanced.ReaderStrategies.ValueTuples
{
    internal class ValueTupleReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public ValueTupleReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return type.FullName != null && type.FullName.StartsWith("System.ValueTuple");
        }

        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            // Create a fast constructor for this tuple type
            // This is called once per type. Since column names aren't important, just the position of the column and
            // the tuple parameter, we don't need to create one for each query. 
            var (compiled, expectedFieldCount) = Compile<TRecord>();
            
            return command => 
            { 
                // This is called at the start of a read
                var rowCount = 0;
                var context = new ValueTupleReaderContext {Column = -1};

                return reader =>
                {
                    // This is called for each row
                    rowCount++;
                    context.Column = 0;
                    
                    if (reader.FieldCount != expectedFieldCount)
                        throw new InvalidOperationException($"Row {rowCount} in the result set has {reader.FieldCount} fields, but it's being mapped to a tuple with {expectedFieldCount} fields. {typeof(TRecord).FullName}");

                    try
                    {
                        return (compiled.Execute(reader, context), true);
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException(rowCount, context.Column, compiled.ExpressionSource, ex);
                    }
                }; 
            };
        }

        // Example, for a tuple of (int, int):
        // 
        //   (reader, context) => 
        //   {
        //       context.Column = 0;                // Only if IncludeColumnNumberInErrors
        //       int value0 = reader.GetInt32(0);
        //       context.Column = 1;                // Only if IncludeColumnNumberInErrors
        //       int value1 = reader.GetInt32(1);
        // 
        //       return new ValueTuple<int, int>(value0, value1); 
        //   }
        (CompiledExpression<ValueTupleReaderFunc<TRecord>> compiled, int expectedFieldCount) Compile<TRecord>()
        {
            var constructors = typeof(TRecord).GetConstructors();
            var tupleConstructor = constructors.Single(p => p.GetParameters().Length == typeof(TRecord).GenericTypeArguments.Length);
            var parameters = tupleConstructor.GetParameters();
            
            var readerArg = Expression.Parameter(typeof(DbDataReader), "reader");
            var contextArg = Expression.Parameter(typeof(ValueTupleReaderContext), "context");

            var locals = new List<ParameterExpression>();
            var body = new List<Expression>();

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                
                var local = Expression.Variable(parameter.ParameterType, "value" + parameter.Name);
                locals.Add(local);

                if (configuration.IncludeColumnNumberInErrors)
                {
                    body.Add(Expression.Assign(Expression.Field(contextArg, nameof(ValueTupleReaderContext.Column)), Expression.Constant(i)));
                }
                
                body.Add(Expression.Assign(local, ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(i), parameter.ParameterType, configuration.TypeHandlerRegistry)));
            }
            
            body.Add(Expression.New(tupleConstructor, locals));

            var lambda = Expression.Lambda<ValueTupleReaderFunc<TRecord>>(
                Expression.Block(locals, body), readerArg, contextArg
            );

            var compiled = ExpressionCompiler.Compile(lambda, configuration.IncludeCompiledReadersInErrors);
            return (compiled, parameters.Length);
        }
    }
}