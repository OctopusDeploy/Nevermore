using System.Collections.Generic;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore
{
    public class TableValuedParameter
    {
        public TableValuedParameter(string typeName, List<SqlDataRecord> dataRecords)
        {
            TypeName = typeName;
            DataRecords = dataRecords;
        }
        
        public string TypeName { get; }
        public List<SqlDataRecord> DataRecords { get; }
    }
}