using System;

namespace Nevermore
{
    public class ReaderException : Exception
    {
        public ReaderException(string message) : base (message)
        {
        }

        public ReaderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}