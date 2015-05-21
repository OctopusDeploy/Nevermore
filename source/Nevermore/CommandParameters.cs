using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace Nevermore
{
    public class CommandParameters : Dictionary<string, object>
    {
        public CommandParameters() : base(StringComparer.OrdinalIgnoreCase)
        {
            CommandType = CommandType.Text;
        }

        public CommandParameters(object args) : this()
        {
            AddFromParametersObject(args);
        }

        public CommandType CommandType { get; set; }

        void AddFromParametersObject(object args)
        {
            if (args == null)
                return;

            var type = args.GetType();
            foreach (var property in type.GetProperties())
            {
                var rw = PropertyReaderFactory.Create<object>(type, property.Name);

                var value = rw.Read(args);
                this[property.Name] = value;
            }
        }

        public virtual void ContributeTo(SqlCommand command)
        {
            command.CommandType = CommandType;
            foreach (var pair in this)
            {
                ContributeParameter(command, pair.Key, pair.Value);
            }
        }

        protected virtual void ContributeParameter(SqlCommand command, string name, object value)
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

                command.CommandText = command.CommandText.Replace("@" + name.TrimStart('@'), "(" + string.Join(", ", inClauseNames.Select(x => "@" + x)) + ")");
                return;
            }

            var columnType = DatabaseTypeConverter.AsDbType(value.GetType());

            var param = new SqlParameter();
            param.ParameterName = name;
            param.DbType = columnType;
            param.Value = value;
            command.Parameters.Add(param);
        }
    }
}