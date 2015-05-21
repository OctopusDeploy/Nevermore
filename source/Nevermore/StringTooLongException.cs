using System;

namespace Nevermore
{
    public class StringTooLongException : Exception
    {
        public StringTooLongException(string message) : base(message)
        {
            
        }
    }
}