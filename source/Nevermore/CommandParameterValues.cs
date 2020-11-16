using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
#if NETFRAMEWORK
using System.Data.SqlClient;
using Microsoft.SqlServer.Server;
#else
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
#endif
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Nevermore.Advanced.PropertyHandlers;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;
using Nevermore.Util;

namespace Nevermore
{
    public class CommandParameterValues : Dictionary<string, object>
    {
        public CommandParameterValues()
            : base(StringComparer.OrdinalIgnoreCase)
        {
            CommandType = CommandType.Text;
        }

        public CommandParameterValues(CommandParameterValues from)
            : base(from, StringComparer.OrdinalIgnoreCase)
        {
            CommandType = from.CommandType;
        }

        public CommandParameterValues(params CommandParameterValues[] from)
            : this()
        {
            if (from.Any())
            {
                CommandType = from.First().CommandType;
            }

            foreach (var values in from)
            {
                AddRange(values);
            }
        }

        public CommandParameterValues(object args)
            : this()
        {
            AddFromParametersObject(args);
        }

        public CommandType CommandType { get; set; }

        public void AddTable<T>(string name, IEnumerable<T> ids)
        {
            var idColumnMetadata = SqlMetaData.InferFromValue(ids.First(), "ParameterValue");

            var dataRecords = ids.Where(v => v != null).Select(v =>
            {
                var record = new SqlDataRecord(idColumnMetadata);
                record.SetValue(0, v);
                return record;
            }).ToList();
            
            AddTable(name, new TableValuedParameter("dbo.[ParameterList]", dataRecords));
        }
        
        public void AddTable(string name, TableValuedParameter tvp)
        {
            Add(name, tvp);
        }
        
        void AddFromParametersObject(object args)
        {
            if (args == null)
                return;

            var type = args.GetType();
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                // TODO: Cache these
                var rw = new PropertyHandler(property);
                var value = rw.Read(args);
                this[property.Name] = value;
            }
        }

        public virtual void ContributeTo(IDbCommand command, ITypeHandlerRegistry typeHandlers, DocumentMap mapping = null)
        {
            command.CommandType = CommandType;
            foreach (var pair in this)
            {
                ContributeParameter(command, typeHandlers, pair.Key, pair.Value, mapping);
            }
        }

        protected virtual void ContributeParameter(IDbCommand command, ITypeHandlerRegistry typeHandlers, string name, object value, DocumentMap mapping = null)
        {
            if (value == null)
            {
                command.Parameters.Add(new SqlParameter(name, DBNull.Value));
                return;
            }

            var typeHandler = typeHandlers.Resolve(value.GetType());
            if (typeHandler != null)
            {
                var p = new SqlParameter(name, SqlDbType.NVarChar);
                typeHandler.WriteDatabase(p, value);
                command.Parameters.Add(p);
                return;
            }
            
            if (value is TableValuedParameter tvp && command is SqlCommand sqlCommand)
            {
                var p = sqlCommand.Parameters.Add(name, SqlDbType.Structured);
                p.Value = tvp.DataRecords;
                p.TypeName = tvp.TypeName;
                return;
            }

            if (value is IEnumerable && (value is string) == false && (value is byte[]) == false)
            {
                var inClauseNames = new List<string>();
                var i = 0;

                var inClauseValues = ((IEnumerable) value).Cast<object>().ToList();
                
                ListExtender.ExtendListRepeatingLastValue(inClauseValues);
                
                foreach (var inClauseValue in inClauseValues)
                {
                    i++;
                    var inClauseName = name + "_" + i;
                    inClauseNames.Add(inClauseName);
                    ContributeParameter(command, typeHandlers, inClauseName, inClauseValue);
                }

                if (i == 0)
                {
                    var inClauseName = name + "_" + i;
                    inClauseNames.Add(inClauseName);
                    ContributeParameter(command, typeHandlers, inClauseName, null);
                }

                var originalParameter = Regex.Escape("@" + name.TrimStart('@')) + @"(?=[^\w\$@#_]|$)";
                var replacementParameters = "(" + string.Join(", ", inClauseNames.Select(x => "@" + x)) + ")";
                command.CommandText = Regex.Replace(command.CommandText, originalParameter, match => replacementParameters, RegexOptions.IgnoreCase);
                return;
            }

            var columnType = DatabaseTypeConverter.AsDbType(value.GetType());
            if (columnType == null)
                throw new InvalidOperationException($"Cannot map type '{value.GetType().FullName}' to a DbType. Consider providing a custom ITypeHandler.");
            
            var param = new SqlParameter();
            param.ParameterName = name;
            param.DbType = columnType.Value;
            param.Value = value;

            if (columnType == DbType.String && value is string text)
            {
                var size = GetBestSizeBucket(text);
                if (size > 0)
                {
                    param.Size = size; 
                }
            }

            // To assist SQL's query plan caching, assign a parameter size for our 
            // common id lookups where possible.
            if (mapping != null
                && mapping.IdColumn != null
                && mapping.IdColumn.MaxLength > 0
                && columnType == DbType.String
                && string.Equals(name, mapping.IdColumn.ColumnName, StringComparison.OrdinalIgnoreCase))
            {
                if (mapping.IdColumn.MaxLength != null)
                {
                    param.Size = mapping.IdColumn.MaxLength.Value;
                }
            }

            if (columnType == DbType.String && mapping != null)
            {
                var indexed = mapping.WritableIndexedColumns();
                var columnMap = indexed.FirstOrDefault(i => string.Equals(i.ColumnName, param.ParameterName, StringComparison.OrdinalIgnoreCase));
                if (columnMap != null && columnMap.MaxLength != null)
                {
                    param.Size = columnMap.MaxLength.Value;
                }
            }

            command.Parameters.Add(param);
        }

        

        // By default all string parameters have their size automatically assigned based on the length of the string.
        // This results in a different query plan depending on the size of the text used. The query plan ends up like this:
        // 
        //   (@firstname nvarchar(24))SELECT TOP 100 *  FROM dbo.[Customer]  WHERE ([FirstName] <> @firstname)  ORDER BY [Id]
        //   (@firstname nvarchar(44))SELECT TOP 100 *  FROM dbo.[Customer]  WHERE ([FirstName] <> @firstname)  ORDER BY [Id]
        //   (@firstname nvarchar(47))SELECT TOP 100 *  FROM dbo.[Customer]  WHERE ([FirstName] <> @firstname)  ORDER BY [Id]
        //
        // So, we will always add a size. We do it in buckets.
        static int GetBestSizeBucket(string text)
        {
            var length = text.Length;
            if (length < 100) return 100;
            if (length < 200) return 200;
            if (length < 600) return 600;
            if (length < 1000) return 1000;
            return 0;    // Use default plan
        }

        public void AddRange(CommandParameterValues other)
        {
            foreach (var item in other)
            {
                if (ContainsKey(item.Key))
                    throw new Exception($"The parameter {item.Key} already exists");
                this[item.Key] = item.Value;
            }
        }
    }
}