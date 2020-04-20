using System.Data;
using System.IO;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal class DataTextReader : TextReader
    {
        readonly IDataReader reader;
        readonly int columnIndex;
        long currentPos;

        public DataTextReader(IDataReader reader, int columnIndex)
        {
            this.reader = reader;
            this.columnIndex = columnIndex;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            var read = (int)reader.GetChars(columnIndex, currentPos, buffer, index, count);
            currentPos += read;
            return read;
        }
    }
}