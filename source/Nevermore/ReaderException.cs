using System;

namespace Nevermore
{
    public class ReaderException : Exception
    {
        public ReaderException(int rowNumber, int fieldNumber, string expressionSource, Exception innerException) : base (Format(rowNumber, fieldNumber, innerException.Message, expressionSource), innerException)
        {
        }

        static string Format(int rowNumber, int fieldNumber, string message, string expressionSource)
        {
            var fieldMessage = fieldNumber >= 0 ? $", column {fieldNumber}" : "";
            return $"Error reading row {rowNumber}{fieldMessage}. {message}.\r\nCompiled reader expression:\r\n\r\n{expressionSource}";
        }
    }    
}