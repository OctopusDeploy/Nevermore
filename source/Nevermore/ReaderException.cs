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
            var note1 = fieldNumber < 0 ? $"\nTo know what column was being read, set {nameof(IRelationalStoreConfiguration.IncludeColumnNumberInErrors)} to true." : "";
            var note2 = expressionSource == null ? $"\nTo see the expression that was reading this data reader, set {nameof(IRelationalStoreConfiguration.IncludeCompiledReadersInErrors)} to true." : $"\r\nCompiled reader expression:\r\n\r\n{expressionSource}";
            return $"Error reading row {rowNumber}{fieldMessage}. {message}.\r\n{note1}\r\n{note2}";
        }
    }    
}