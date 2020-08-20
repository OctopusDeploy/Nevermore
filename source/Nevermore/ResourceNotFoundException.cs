using System;

namespace Nevermore
{
    public class ResourceNotFoundException : Exception
    {
        public ResourceNotFoundException()
            : base("The resource was not found.")
        {

        }

        public ResourceNotFoundException(object resourceId)
           : base("The resource '" + resourceId + "' was not found.")
        {
        }
    }
}