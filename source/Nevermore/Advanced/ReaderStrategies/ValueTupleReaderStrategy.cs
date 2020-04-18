using System;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using Nevermore.Util;

namespace Nevermore.Advanced.ReaderStrategies
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
            // This is called once per type
            var constructors = typeof(TRecord).GetConstructors();
            var tupleConstructor = constructors.Single(p => p.GetParameters().Length == typeof(TRecord).GenericTypeArguments.Length);
            
            var readerArg = Expression.Parameter(typeof(DbDataReader), "reader");
            var parameters = tupleConstructor.GetParameters();
            
            // Example, for a tuple of (int, int):
            //   reader => new ValueTuple<string, string>(reader.GetInt32(0),reader.GetInt32(1)); 
            var constructorFunc = Expression.Lambda<Func<DbDataReader, TRecord>>(
                Expression.New(tupleConstructor,
                    parameters.Select((parameterInfo, i) =>
                        ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(i),
                            parameterInfo.ParameterType, configuration.TypeHandlerRegistry))
                ), readerArg
            ).Compile();

            var expectedFieldCount = parameters.Length;
            
            return command => 
            { 
                // This is called at the start of a read
                var rowCount = 0;
                
                return reader =>
                {
                    // This is called for each row
                    rowCount++;
                    if (reader.FieldCount != expectedFieldCount)
                        throw new InvalidOperationException($"Row {rowCount} in the result set has {reader.FieldCount} fields, but it's being mapped to a tuple with {expectedFieldCount} fields. {typeof(TRecord).FullName}");

                    try
                    {
                        return (constructorFunc(reader), true);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error reading row {rowCount}: {ex.Message}", ex);
                    }
                }; 
            };
        }
    }
}