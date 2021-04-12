using System;

namespace Nevermore
{
    public class StaleDataException : Exception
    {
        public StaleDataException(string message)
            : base(message)
        {
        }
    }
}