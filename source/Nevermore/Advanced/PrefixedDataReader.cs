using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Nevermore.Advanced
{
    class PrefixedDataReader : DbDataReader
    {
        readonly string prefix;
        readonly DbDataReader innerReader;
        bool initialized;
        int[] fields;
        
        public PrefixedDataReader(string prefix, DbDataReader reader)
        {
            this.prefix = prefix;
            innerReader = reader;
            
            Initialize();
        }
            
        void Initialize()
        {
            if (initialized)
                return;

            initialized = true;

            var translatedFields = new List<int>();
            
            for (var i = 0; i < innerReader.FieldCount; i++)
            {
                var name = innerReader.GetName(i);
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    translatedFields.Add(i);    // E.g., 0 -> 7, 1 -> 8, 10 -> 14
            }

            fields = translatedFields.ToArray();
        }

        int Translate(int ordinal)
        {
            return fields[ordinal];
        }
        
        int ReverseTranslate(int sourceOrdinal)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                if (fields[i] == sourceOrdinal)
                    return i;
            }

            return -1;
        }
        
        public override bool GetBoolean(int ordinal)
        {
            return innerReader.GetBoolean(Translate(ordinal));
        }

        public override byte GetByte(int ordinal)
        {
            return innerReader.GetByte(Translate(ordinal));
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length)
        {
            return innerReader.GetBytes(Translate(ordinal), dataOffset, buffer, bufferOffset, length);
        }

        public override char GetChar(int ordinal)
        {
            return innerReader.GetChar(Translate(ordinal));
        }

        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length)
        {
            return innerReader.GetChars(Translate(ordinal), dataOffset, buffer, bufferOffset, length);
        }

        public override string GetDataTypeName(int ordinal)
        {
            return innerReader.GetDataTypeName(Translate(ordinal));
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return innerReader.GetDateTime(Translate(ordinal));
        }

        public override decimal GetDecimal(int ordinal)
        {
            return innerReader.GetDecimal(Translate(ordinal));
        }

        public override double GetDouble(int ordinal)
        {
            return innerReader.GetDouble(Translate(ordinal));
        }

        public override Type GetFieldType(int ordinal)
        {
            return innerReader.GetFieldType(Translate(ordinal));
        }

        public override float GetFloat(int ordinal)
        {
            return innerReader.GetFloat(Translate(ordinal));
        }

        public override Guid GetGuid(int ordinal)
        {
            return innerReader.GetGuid(Translate(ordinal));
        }

        public override short GetInt16(int ordinal)
        {
            return innerReader.GetInt16(Translate(ordinal));
        }

        public override int GetInt32(int ordinal)
        {
            return innerReader.GetInt32(Translate(ordinal));
        }

        public override long GetInt64(int ordinal)
        {
            return innerReader.GetInt64(Translate(ordinal));
        }

        public override string GetName(int ordinal)
        {
            return innerReader.GetName(Translate(ordinal)).Substring(prefix.Length);
        }

        public override int GetOrdinal(string name)
        {
            return ReverseTranslate(innerReader.GetOrdinal(prefix + name));
        }

        public override string GetString(int ordinal)
        {
            return innerReader.GetString(Translate(ordinal));
        }

        public override object GetValue(int ordinal)
        {
            return innerReader.GetValue(Translate(ordinal));
        }

        public override int GetValues(object[] values)
        {
            return innerReader.GetValues(values);
        }

        public override bool IsDBNull(int ordinal)
        {
            return innerReader.IsDBNull(Translate(ordinal));
        }

        public override int FieldCount => fields.Length;

        public override object this[int ordinal] => innerReader[Translate(ordinal)];

        public override object this[string name] => innerReader[prefix + name];

        public override int RecordsAffected => innerReader.RecordsAffected;

        public override bool HasRows => innerReader.HasRows;
        public override bool IsClosed => innerReader.IsClosed;

        public override bool NextResult()
        {
            return innerReader.NextResult();
        }

        public override bool Read()
        {
            return innerReader.Read();
        }

        public override int Depth => innerReader.Depth;

        public override IEnumerator GetEnumerator()
        {
            return innerReader.GetEnumerator();
        }
    }
}