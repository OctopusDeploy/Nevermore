using System;
using System.Buffers;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Nevermore.Advanced.ReaderStrategies
{
    internal class DataReaderTextStream : Stream
    {
        readonly IDataReader reader;
        readonly int columnIndex;
        readonly ArrayPool<char> pool;
        readonly char[] charBuffer;
        long currentPos;

        public const int CharBufferSize = 256;

        public DataReaderTextStream(IDataReader reader, int columnIndex)
        {
            this.reader = reader;
            this.columnIndex = columnIndex;
                
            pool = ArrayPool<char>.Shared;
            charBuffer = pool.Rent(CharBufferSize);
        }
            
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0) throw new InvalidOperationException("Nevermore string stream doesn't support reading from a non-zero offset.");
            Debug.Assert(count >= CharBufferSize * 4, "Unexpected buffer size - count must be at least 4 * CharBufferSize");
                
            // We cannot re-read a char from the data reader. Since chars could be up to 4 bytes, we only read 256
            // chars from the data reader, and we expect the JSON reader calling this to request at least 1024
            // bytes. This way we know the chars we read can fit directly into the buffer.
            var read = (int)reader.GetChars(columnIndex, currentPos, charBuffer, 0, CharBufferSize);
            
            // Keep track of where we've read to, as we can't read the same chars twice 
            currentPos += read;

            return Encoding.UTF8.GetBytes(charBuffer, 0, read, buffer, 0);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return 0;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get; set; }

        protected override void Dispose(bool disposing)
        {
            pool.Return(charBuffer);
            base.Dispose(disposing);
        }
    }
}