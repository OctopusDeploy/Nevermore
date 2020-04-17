using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Nevermore.Advanced.TypeHandlers;

namespace Nevermore.IntegrationTests.Contracts
{
    public class ReferenceCollectionTypeHandler : ITypeHandler
    {
        public bool CanConvert(Type objectType)
        {
            return typeof(ReferenceCollection).IsAssignableFrom(objectType);
        }

        public object ReadDatabase(DbDataReader reader, int columnIndex)
        {
            if (reader.IsDBNull(columnIndex)) 
                return new ReferenceCollection();
            var text = reader.GetString(columnIndex);
            if (string.IsNullOrWhiteSpace(text))
                return new ReferenceCollection();
            
            return new ReferenceCollection(Parse(text));
        }

        public void WriteDatabase(DbParameter parameter, object value)
        {
            parameter.DbType = DbType.String;
            if (value is ReferenceCollection coll)
            {
                parameter.Value = Format(coll);
            }
            else
            {
                parameter.Value = "";
            }
        }

        public static IEnumerable<string> Parse(string value)
        {
            return (value ?? string.Empty).Split('|').Where(item => !string.IsNullOrWhiteSpace(item));
        }

        public static string Format(IEnumerable<string> items)
        {
            return $"|{string.Join("|", items)}|";
        }
    }
}