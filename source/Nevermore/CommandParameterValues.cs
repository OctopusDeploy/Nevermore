using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Nevermore.Mapping;

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

        void AddFromParametersObject(object args)
        {
            if (args == null)
                return;

            var type = args.GetType();
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                var rw = PropertyReaderFactory.Create<object>(type, property.Name);

                var value = rw.Read(args);
                this[property.Name] = value;
            }
        }

        public virtual void ContributeTo(IDbCommand command, DocumentMap mapping = null)
        {
            command.CommandType = CommandType;
            foreach (var pair in this)
            {
                ContributeParameter(command, pair.Key, pair.Value, mapping);
            }
        }

        protected virtual void ContributeParameter(IDbCommand command, string name, object value, DocumentMap mapping = null)
        {
            if (value == null)
            {
                command.Parameters.Add(new SqlParameter(name, DBNull.Value));
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
                    ContributeParameter(command, inClauseName, inClauseValue);
                    i++;
                }

                if (i == 0)
                {
                    var inClauseName = name + "_" + i;
                    inClauseNames.Add(inClauseName);
                    ContributeParameter(command, inClauseName, null);
                }


                var originalParameter = Regex.Escape("@" + name.TrimStart('@')) + @"(?=[^\w\$@#_]|$)";
                var replacementParameters = "(" + string.Join(", ", inClauseNames.Select(x => "@" + x)) + ")";
                command.CommandText = Regex.Replace(command.CommandText, originalParameter, match => replacementParameters, RegexOptions.IgnoreCase);
                return;
            }

            var columnType = DatabaseTypeConverter.AsDbType(value.GetType());

            var param = new SqlParameter();
            param.ParameterName = name;
            param.DbType = columnType;
            param.Value = value;

            // To assist SQL's query plan caching, assign a parameter size for our 
            // common id lookups where possible.
            if (mapping != null
                && mapping.IdColumn != null
                && mapping.IdColumn.MaxLength > 0
                && columnType == DbType.String
                && string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                param.Size = mapping.IdColumn.MaxLength;
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