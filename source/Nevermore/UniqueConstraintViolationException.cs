using System;

namespace Nevermore
{
    public class UniqueConstraintViolationException : Exception
    {
        public UniqueConstraintViolationException(string message)
            : base(message)
        {
        }
    }
}