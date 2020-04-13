using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Nevermore.Util
{
    internal static class ExceptionExtensions
    {
        static Exception UnpackFromContainers(this Exception error)
        {
            var aggregateException = error as AggregateException;
            if (aggregateException != null && aggregateException.InnerExceptions.Count == 1)
            {
                return UnpackFromContainers(aggregateException.InnerExceptions[0]);
            }

            if (error is TargetInvocationException && error.InnerException != null)
            {
                return UnpackFromContainers(error.InnerException);
            }

            return error;
        }
        
        public static string GetErrorSummary(this Exception error)
        {
            error = error.UnpackFromContainers();

            if (error is TaskCanceledException || error is OperationCanceledException)
                return "The task was canceled.";

            return error.Message;
        }

    }
}
