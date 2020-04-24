using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.PropertyHandlers;
using Nevermore.Advanced.ReaderStrategies.Compilation;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    // This class is used to build an expression tree that can instantiate a given class, mapping each field in the data
    // reader, as if it were hand-coded.
    // Here's an example. 
    // Suppose the type being read is:
    // 
    //   abstract class Contact
    //   { 
    //      public string Id { get;set; }   
    //      public string FirstName { get;set; }   
    //      public string LastName { get;set; }
    //      public int? Age { get;set; }
    //      public Uri Website { get;set; }
    //      public string Type { get;set; }
    //      public string[] Tags { get;set; }
    //   }
    //   
    //   With the select statement returning these columns:
    //   
    //       Id, FirstName, LastName, Age, Website, Type, JSON, JSONBlob
    //   
    //   We want to generate something like:
    // 
    //       (DbDataReader reader, DocumentReaderContext context) =>
    //       {
    //           Contact result;
    //           
    //           // 'readers' section
    //           var temp0 = reader.GetString(0);                                     // Id
    //           var temp1 = reader.GetString(1);                                     // FirstName
    //           var temp2 = reader.GetString(1);                                     // LastName
    //           var temp3 = reader.IsDbNull(3) ? null : (int?)reader.GetInt32(3);    // Age
    //           var temp4 = // call type handler for URI                             // Website
    //           var tempTypeValue = reader.GetString(5);                                  // Type (special handling)
    // 
    //           var deserializeAsType = context.ResolveType(tempTypeValue);
    //           var jsonResult = context.DeserializeText<Contact>(reader, 7, deserializeAsType);                       // If JSON column          
    //           var jsonBlobResult = context.DeserializeCompressed<Contact>(reader, 8, deserializeAsType);             // If JSONBlob column
    //           
    //           result = context.SelectPreferredResult<Contact>(jsonResult, jsonBlobResult);                           // If JSON and JSONBlob column
    //           
    //           if (result != null)
    //           {
    //               // 'assigners' section
    //               result.Id = temp0;
    //               result.FirstName = temp1;
    //               result.LastName = temp2;
    //               result.Age = temp3;
    //               propertyHandler1.Write(result, temp4);                // Assuming Website has a custom IPropertyHandler on the column mapping
    //               result.Type = tempType;
    //           }
    //  
    //           return result;
    //       }

    internal class DocumentReaderExpressionBuilder
    {
        const BindingFlags BindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        readonly Type type;
        readonly DocumentMap map;
        readonly ITypeHandlerRegistry typeHandlers;

        // Arguments to the final func
        readonly ParameterExpression dataReaderArgument;
        readonly ParameterExpression contextArgument;
        readonly ParameterExpression result;
        
        readonly List<ParameterExpression> locals = new List<ParameterExpression>();
        readonly List<Expression> readers = new List<Expression>();
        readonly List<Expression> assigners = new List<Expression>();
        readonly ParameterExpression deserializeAsLocal;
        readonly MethodInfo deserializeTextMethod;
        readonly MethodInfo deserializeCompressedMethod;
        readonly MethodInfo resolveTypeMethod;
        readonly MethodInfo selectPreferredResultMethod;
        readonly MethodInfo propertyHandlerWriteMethod;
        readonly FieldInfo columnField;
        
        // Locals that may or may not be declared, depending on whether they are in the result set
        ParameterExpression typeValueLocal;
        ParameterExpression jsonResult;
        ParameterExpression jsonBlobResult;

        int idIndex = -1;
        int typeIndex = -1;
        int jsonIndex = -1;
        int jsonBlobIndex = -1;
        
        public DocumentReaderExpressionBuilder(DocumentMap map, ITypeHandlerRegistry typeHandlers)
        {
            type = map.Type;
            this.map = map;
            this.typeHandlers = typeHandlers;

            var contextType = typeof(DocumentReaderContext);
            dataReaderArgument = Expression.Parameter(typeof(DbDataReader), "reader");
            contextArgument = Expression.Parameter(contextType, "context");
            result = DeclareLocal(map.Type, "result");
            deserializeAsLocal = DeclareLocal(typeof(Type), "deserializeAsType");

            columnField = contextType.GetField(nameof(DocumentReaderContext.Column));
            deserializeTextMethod = contextType.GetMethod(nameof(DocumentReaderContext.DeserializeText), BindingFlags)?.MakeGenericMethod(type);
            deserializeCompressedMethod = contextType.GetMethod(nameof(DocumentReaderContext.DeserializeCompressed), BindingFlags)?.MakeGenericMethod(type);
            resolveTypeMethod = contextType.GetMethod(nameof(DocumentReaderContext.ResolveType), BindingFlags);
            selectPreferredResultMethod = contextType.GetMethod(nameof(DocumentReaderContext.SelectPreferredResult), BindingFlags)?.MakeGenericMethod(type);
            propertyHandlerWriteMethod = typeof(IPropertyHandler).GetMethod(nameof(IPropertyHandler.Write), BindingFlags);
            
            if (columnField == null || deserializeTextMethod == null || deserializeCompressedMethod == null || resolveTypeMethod == null || selectPreferredResultMethod == null || propertyHandlerWriteMethod == null)
                throw new InvalidOperationException("Could not find one or more required methods.");
        }

        public void Id(int i, ColumnMapping column)
        {
            idIndex = i;
            Column(i, column);
        }
        
        public void Column(int i, ColumnMapping column)
        {
            var local = Expression.Variable(column.Type, "temp" + i);
            locals.Add(local);
            TrackColumn(i);
            readers.Add(Expression.Assign(local, ExpressionHelper.GetValueFromReaderAsType(dataReaderArgument, Expression.Constant(i), column.Type, typeHandlers)));

            AddAssigner(column, local);
        }

        public void TypeColumn(int i, ColumnMapping column)
        {
            typeIndex = i;
            if (column != null)
            {
                typeValueLocal = Expression.Variable(column.Type, "tempType");
                locals.Add(typeValueLocal);
                TrackColumn(i);
                readers.Add(Expression.Assign(typeValueLocal, ExpressionHelper.GetValueFromReaderAsType(dataReaderArgument, Expression.Constant(i), column.Type, typeHandlers)));
                
                AddAssigner(column, typeValueLocal);
            }
            else
            {
                typeValueLocal = Expression.Variable(typeof(string), "tempType");
                locals.Add(typeValueLocal);
                TrackColumn(i);
                readers.Add(Expression.Assign(typeValueLocal, ExpressionHelper.GetValueFromReaderAsType(dataReaderArgument, Expression.Constant(i), typeof(string), typeHandlers)));
            }
            
            readers.Add(Expression.Assign(deserializeAsLocal, Expression.Call(contextArgument, resolveTypeMethod, Expression.Convert(typeValueLocal, typeof(object)))));
        }

        public void JsonColumn(int i)
        {
            jsonIndex = i;
            jsonResult = Expression.Variable(type, "deserializedFromJson");
            locals.Add(jsonResult);
            TrackColumn(i);
            var deserialize = Expression.Call(contextArgument, deserializeTextMethod, dataReaderArgument, Expression.Constant(i), deserializeAsLocal);
            readers.Add(Expression.Assign(jsonResult, deserialize));
        }
        
        public void JsonBlobColumn(int i)
        {
            jsonBlobIndex = i;
            jsonBlobResult = Expression.Variable(type, "deserializedFromJsonBlob");
            locals.Add(jsonBlobResult);
            TrackColumn(i);
            var deserialize = Expression.Call(contextArgument, deserializeCompressedMethod, dataReaderArgument, Expression.Constant(i), deserializeAsLocal);
            readers.Add(Expression.Assign(jsonBlobResult, deserialize));
        }

        void AddAssigner(ColumnMapping column, ParameterExpression local)
        {
            if (column.Direction == ColumnDirection.ToDatabase)
                // We don't read this field
                return;

            if (column.Property != null && column.PropertyHandler is PropertyHandler)
            {
                // Optimization: Rather than call the property handler, we can embed the expression to assign the
                // property directly. This avoids a few casts to and from object.
                assigners.Add(Expression.Assign(Expression.Property(result, column.Property), local));
            }
            else
            {
                assigners.Add(Expression.Call(Expression.Constant(column.PropertyHandler, typeof(IPropertyHandler)), propertyHandlerWriteMethod, result, Expression.Convert(local, typeof(object))));
            }
        }

        public Expression<DocumentReaderFunc> Build()
        {
            AssertValidColumnOrdering();
            
            var body = new List<Expression>();

            if (typeValueLocal == null)
                body.Add(Expression.Assign(deserializeAsLocal, Expression.Constant(type)));
            
            body.AddRange(readers);

            if (jsonResult != null && jsonBlobResult == null)
            {
                body.Add(Expression.Assign(result, jsonResult));
            }
            else if (jsonBlobResult != null && jsonResult == null)
            {
                body.Add(Expression.Assign(result, jsonBlobResult));
            }
            else if (jsonResult != null && jsonBlobResult != null)
            {
                body.Add(Expression.Assign(result, Expression.Call(contextArgument, selectPreferredResultMethod, jsonResult, jsonBlobResult)));
            }

            body.Add(Expression.IfThen(Expression.NotEqual(result, Expression.Constant(null, type)), 
                Expression.Block(assigners)
                ));

            body.Add(result);

            var block = Expression.Block(
                locals,
                body);

            var lambda = Expression.Lambda<DocumentReaderFunc>(block, dataReaderArgument, contextArgument);
            return lambda;
        }
        
        ParameterExpression DeclareLocal(Type variableType, string name)
        {
            var local = Expression.Variable(variableType, name);
            locals.Add(local);
            return local;
        }
        
        void TrackColumn(int i)
        {
            readers.Add(Expression.Assign(Expression.Field(contextArgument, columnField), Expression.Constant(i)));
        }

        void AssertValidColumnOrdering()
        {
            var storageFormat = map.JsonStorageFormat;
            var expectsJson = map.JsonStorageFormat != JsonStorageFormat.CompressedOnly;
            var expectsJsonBlob = map.JsonStorageFormat != JsonStorageFormat.TextOnly;
            var expectsType = map.TypeResolutionColumn != null;
            var name = map.Type.Name;

            if (idIndex < 0)
                throw Fail($"The class '{name}' has a document map, but the query does not include the '{map.IdColumn.ColumnName}' column. Queries against this type must include the '{map.IdColumn.ColumnName}' in the select clause.");
            
            if (expectsJson && jsonIndex < 0 && expectsJsonBlob && jsonBlobIndex < 0)
                throw Fail($"The class '{name}' has a document map with JSON storage set to {storageFormat.ToString()}, but the query does not include either the 'JSON' or 'JSONBlob' column. Queries against this type must include both columns in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support.");

            if (expectsJson && jsonIndex < 0)
                throw Fail($"The class '{name}' has a document map with JSON storage set to {storageFormat.ToString()}, but the query does not include the 'JSON' column. Queries against this type must include the JSON in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support.");
            
            if (expectsJsonBlob && jsonBlobIndex < 0)
                throw Fail($"The class '{name}' has a document map with JSON storage set to {storageFormat.ToString()}, but the query does not include the 'JSONBlob' column. Queries against this type must include the JSONBlob in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support.");

            if (expectsType && typeIndex < 0)
                throw Fail($"When querying the document '{name}', the '{map.TypeResolutionColumn.ColumnName}' column must always be included. Change the select clause to include the '{map.TypeResolutionColumn.ColumnName}' column.");

            if (typeIndex >= 0 && expectsJson && typeIndex > jsonIndex)
                throw Fail($"When querying the document '{name}', the 'Type' column must always appear before the 'JSON' column. Change the order in the SELECT clause, or if selecting '*', change the order of columns in the table.");
            
            if (typeIndex >= 0 && expectsJsonBlob && typeIndex > jsonBlobIndex)
                throw Fail($"When querying the document '{name}', the 'Type' column must always appear before the 'JSONBlob' column. Change the order in the SELECT clause, or if selecting '*', change the order of columns in the table.");
        }

        static Exception Fail(string message)
        {
            return new InvalidOperationException(message);
        }
    }
}