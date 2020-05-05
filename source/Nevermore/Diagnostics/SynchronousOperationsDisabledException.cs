using System;

namespace Nevermore.Diagnostics
{
    public class SynchronousOperationsDisabledException : Exception
    {
        public SynchronousOperationsDisabledException() : base("Synchronous database operations in Nevermore are disabled. Set AllowSynchronousOperations to true to allow these actions, or change the code to call the Async version of the operation. ")
        {
            
        }
    }
}