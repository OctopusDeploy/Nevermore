using System;

namespace Nevermore.Diagnostics
{
    public class DuplicateQueryException : Exception
    {
        public DuplicateQueryException(string message) : base(message)
        {
        }
    }
}