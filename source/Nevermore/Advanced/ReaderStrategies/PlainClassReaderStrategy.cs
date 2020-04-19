using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore.Advanced.ReaderStrategies
{
    /// <summary>
    /// This strategy is used for "POCO", or "Plain-Old-CLR-Objects" classes (those that don't have a document map).
    /// They will be read from the reader automatically. Our requirements are:
    /// 
    ///  - The class must provide a constructor (declared or not) with no parameters.
    ///  - We only bind public, settable properties
    ///  - Column names must match property names, but the casing does not need to match
    ///  - If a column exists in the results, a property must exist on the class
    ///  - A property on the class, however, does not need to exist as a column (it will simply not be set)
    /// </summary>
    internal class PlainClassReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public PlainClassReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return 
                type.IsClass 
                && type.GetConstructors().Any(c => c.IsPublic && c.GetParameters().Length == 0)
                && !configuration.Mappings.ResolveOptional(type, out _);
        }
        
        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            // This is called once for each class we read, and cached. To make it fast - as fast as if we wrote it by
            // hand - we generate and compile C# expression trees for each property on the class, and one to call the
            // constructor.
            
            // Find the parameter-less constructor
            var constructors = typeof(TRecord).GetConstructors();
            var defaultConstructor = constructors.FirstOrDefault(p => p.GetParameters().Length == 0) ?? throw new InvalidOperationException("When reading query results to a class, the class must provide a default constructor with no parameters.");
            var constructor = Expression.New(defaultConstructor);
            var resultLocalVariable = Expression.Variable(typeof(TRecord), "result");
            var createAndAssignResult = Expression.Assign(resultLocalVariable, constructor);
            
            // Create fast setters for all properties on the type
            var properties = typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);
            
            var readerParameter = Expression.Parameter(typeof(DbDataReader), "reader");

                
            // TODO: Cache this per query like in document strategy
            Func<TRecord, DbDataReader, TRecord> compiledFunc = null;
            // Called once for each time Stream() is called
            var expectedFieldCount = 0;
            
            return command =>
            {
                var rowNumber = 0;
                
                return reader =>
                {
                    // Called once per row
                    rowNumber++;
                    
                    // We cached property setters for each property on the class, but we don't know what order they 
                    // will be in the SELECT statement, or if all properties are used. So the first chance we get, 
                    // we'll create an array of the property setters in the correct order based on the result set. We 
                    // do this on row 1, and store it in the `assigners` array.
                    if (compiledFunc == null)
                    {
                        expectedFieldCount = reader.FieldCount;
                        
                        var expressions = new List<Expression>(expectedFieldCount + 2);
                        expressions.Add(createAndAssignResult);
                        
                        for (var i = 0; i < reader.FieldCount; i++)
                        {
                            var name = reader.GetName(i);
                            var property = properties.FirstOrDefault(p => p.Name == name);
                            if (property != null)
                            {
                                var assignPropertyToValue = Expression.Assign(
                                    Expression.Property(resultLocalVariable, property),
                                    ExpressionHelper.GetValueFromReaderAsType(readerParameter, Expression.Constant(i), property.PropertyType, configuration.TypeHandlerRegistry));
                                
                                expressions.Add(assignPropertyToValue);
                            }
                            else
                            {
                                throw new Exception($"The query returned a column named '{name}' but no property by that name exists on the target type '{typeof(TRecord).Name}'");
                            }
                        }

                        expressions.Add(resultLocalVariable);
                        
                        var block = Expression.Block(
                            expressions
                        );

                        var lambda = Expression.Lambda<Func<TRecord, DbDataReader, TRecord>>(block, resultLocalVariable, readerParameter);
                        compiledFunc = lambda.Compile();
                    }

                    // Assertion - in case the result set somehow returns rows with different numbers of columns
                    if (reader.FieldCount != expectedFieldCount)
                        throw new ReaderException($"Row {rowNumber} in the result set has {reader.FieldCount} columns, but the first row had {expectedFieldCount} columns. You cannot change the number of columns in a single result set. {typeof(TRecord).FullName}");

                    try
                    {
                        var instance = compiledFunc(default, reader);
                        return (instance, true);
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException($"Error reading row {rowNumber}: {ex.Message}", ex);
                    }
                };
            };
            
        }

    }
}