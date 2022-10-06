using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.ReaderStrategies.Compilation;

namespace Nevermore.Advanced.ReaderStrategies.ArbitraryClasses
{
    /// <summary>
    /// This strategy is used for "POCO", or "Plain-Old-CLR-Objects" classes (those that don't have a document map).
    /// They will be read from the reader automatically. Our requirements are:
    /// 
    ///  - The class must provide a constructor (declared or not) with no parameters, or a list of parameters that exactly matches the fields on the reader by type
    ///  - We only bind public, settable properties
    ///  - Column names must match property names, but the casing does not need to match
    ///  - If a column exists in the results, a property must exist on the class
    ///  - A property on the class, however, does not need to exist as a column (it will simply not be set)
    /// </summary>
    internal class ArbitraryClassReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public ArbitraryClassReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return type.IsClass && !configuration.DocumentMaps.ResolveOptional(type, out _);
        }
        
        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            var cache = new ConcurrentDictionary<int, CompiledExpression<ArbitraryClassReaderFunc<TRecord>>>();
            
            return command =>
            {
                CompiledExpression<ArbitraryClassReaderFunc<TRecord>> compiled = null;
                var rowNumber = 0;
                var context = new ArbitraryClassReaderContext();

                return reader =>
                {
                    rowNumber++;
                    
                    if (compiled == null)
                    {
                        // We compile a C# expression tree every time we hit a new query. 
                        // We cache it by the SQL statement that produced it (since that will determine the columns) and
                        // the field count, in case there is a select(*) and the number of columns returned changes somehow.
                        compiled = cache.GetOrAdd(HashCode.Combine(command.Statement, reader.FieldCount), _ => Compile<TRecord>(reader));
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

        // Example: given a class Person { string FirstName; string LastName }
        // 
        //     (DbDataReader reader, ArbitraryClassReaderContext context) => 
        //     {
        //         var result = new Person();
        //         result.FirstName = reader.GetString(0);
        //         result.LastName = reader.GetString(1);
        //         return result;
        //     }
        //
        //   -- OR --
        //
        //     (DbDataReader reader, ArbitraryClassReaderContext context) =>
        //     {
        //         var result = new Person(reader.GetString(0), reader.GetString(1));
        //         return result;
        //     }
        CompiledExpression<ArbitraryClassReaderFunc<TRecord>> Compile<TRecord>(IDataRecord record)
        {
            // To make it fast - as fast as if we wrote it by hand - we generate and compile C# expression trees for
            // each property on the class, and one to call the constructor.

            var constructors = typeof(TRecord).GetConstructors();
            // Find the parameter-less constructor
            var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            // Find a parameterized constructor that matches the data reader
            var parameterizedConstructor = (from ctor in constructors
                let parameters = ctor.GetParameters()
                where parameters.Length == record.FieldCount && MatchesTypes(parameters, record)
                select ctor).FirstOrDefault();
            var selectedConstructor = parameterizedConstructor
                                      ?? defaultConstructor
                                      ?? throw new InvalidOperationException("No default constructor or constructor that exactly matches the number and types of the reader fields was found.");

            var readerArg = Expression.Parameter(typeof(DbDataReader), "reader");
            var contextArg = Expression.Parameter(typeof(ArbitraryClassReaderContext), "context");

            var locals = new List<ParameterExpression>();
            var body = new List<Expression>();
            
            var resultLocalVariable = Expression.Variable(typeof(TRecord), "result");
            locals.Add(resultLocalVariable);

            if (selectedConstructor.GetParameters().Any())
            {
                BuildParameterizedConstructorExpression(selectedConstructor, readerArg, body, resultLocalVariable);
            }
            else
            {
                BuildParameterlessConstructorExpression<TRecord>(record, body, resultLocalVariable, selectedConstructor, contextArg, readerArg);
            }

            // Return it
            body.Add(resultLocalVariable);

            var block = Expression.Block(
                locals,
                body
            );

            var lambda = Expression.Lambda<ArbitraryClassReaderFunc<TRecord>>(block, readerArg, contextArg);

            return ExpressionCompiler.Compile(lambda);
        }

        void BuildParameterlessConstructorExpression<TRecord>(IDataRecord record, List<Expression> body, ParameterExpression resultLocalVariable, ConstructorInfo selectedConstructor, ParameterExpression contextArg, ParameterExpression readerArg)
        {
            // Create fast setters for all properties on the type
            var properties = typeof(TRecord).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty);

            body.Add(Expression.Assign(resultLocalVariable, Expression.New(selectedConstructor)));

            var expectedFieldCount = record.FieldCount;
            for (var i = 0; i < expectedFieldCount; i++)
            {
                var name = record.GetName(i);

                var property = properties.FirstOrDefault(p => p.Name == name);
                if (property != null)
                {
                    body.Add(Expression.Assign(Expression.Field(contextArg, nameof(ArbitraryClassReaderContext.Column)), Expression.Constant(i)));

                    body.Add(Expression.Assign(
                        Expression.Property(resultLocalVariable, property),
                        ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(i), property.PropertyType, configuration.TypeHandlers)));
                }
                else
                {
                    throw new Exception($"The query returned a column named '{name}' but no property by that name exists on the target type '{typeof(TRecord).Name}'. When reading to an arbitrary type, all columns must have a matching settable property.");
                }
            }
        }

        void BuildParameterizedConstructorExpression(ConstructorInfo selectedConstructor, ParameterExpression readerArg, List<Expression> body, ParameterExpression resultLocalVariable)
        {
            var arguments = selectedConstructor
                .GetParameters()
                .Select((p, i) => ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(i), p.ParameterType, configuration.TypeHandlers));
            body.Add(Expression.Assign(resultLocalVariable, Expression.New(selectedConstructor, arguments)));
        }

        static bool MatchesTypes(IEnumerable<ParameterInfo> parameters, IDataRecord record)
        {
            return !parameters.Where((t, i) => t.ParameterType != record.GetFieldType(i)).Any();
        }
    }
}