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

        public void AddTable(string name, IEnumerable<string> ids)
        {
            var idColumnMetadata = new SqlMetaData("ParameterValue", SqlDbType.NVarChar, 300);
            
            Add(name, ids.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v =>
            {
                var record = new SqlDataRecord(idColumnMetadata);
                record.SetValue(0, v);
                return record;
            }).ToList());
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
            
            if (value is List<SqlDataRecord> dr && command is SqlCommand sqlCommand)
            {
                var p = sqlCommand.Parameters.Add(name, SqlDbType.Structured);
                p.Value = dr;
                p.TypeName = "dbo.[ParameterList]";
                return;
            }

            if (value is IEnumerable && (value is string) == false && (value is byte[]) == false)
            {
                var inClauseNames = new List<string>();
                var i = 0;
                foreach (var inClauseValue in (IEnumerable)value)
                {
                    var inClauseName = name + "_" + i;
                    inClauseNames.Add(inClauseName);
                    ContributeParameter(command, typeHandlers, inClauseName, inClauseValue);
                    i++;
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

            // To assist SQL's query plan caching, assign a parameter size for our 
            // common id lookups where possible.
            if (mapping != null
                && mapping.IdColumn != null
                && mapping.IdColumn.MaxLength > 0
                && columnType == DbType.String
                && string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                if (mapping.IdColumn.MaxLength != null)
                {
                    param.Size = mapping.IdColumn.MaxLength.Value;
                }
            }

            command.Parameters.Add(param);
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