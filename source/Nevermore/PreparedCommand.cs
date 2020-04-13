using System;
using Nevermore.Mapping;

namespace Nevermore.Util
{
    public class PreparedCommand
    {
        public PreparedCommand(string statement, CommandParameterValues parameterValues, RetriableOperation operation = RetriableOperation.None, DocumentMap mapping = null, TimeSpan? commandTimeout = null)
        {
            Mapping = mapping;
            Statement = statement;
            ParameterValues = parameterValues;
            Operation = operation;
            CommandTimeout = commandTimeout;
        }
        
        public string Statement { get; }
        public DocumentMap Mapping { get; }
        public CommandParameterValues ParameterValues { get; }
        public RetriableOperation Operation { get; }
        public TimeSpan? CommandTimeout { get; }
    }
}