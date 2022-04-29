using System;
using System.Runtime.Serialization;

namespace Nevermore.Advanced
{
    [Serializable]
    public class DeadlockException : Exception
    {
        public DeadlockException()
        {
        }

        public DeadlockException(string message) : base(message)
        {
        }

        public DeadlockException(string message, Exception inner) : base(message, inner)
        {
        }

        protected DeadlockException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}