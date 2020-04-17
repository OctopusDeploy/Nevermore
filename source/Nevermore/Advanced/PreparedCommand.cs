using System;
using System.Data;
using Nevermore.Mapping;

namespace Nevermore.Advanced
{
    public class PreparedCommand
    {
        public PreparedCommand(string statement, CommandParameterValues parameterValues, RetriableOperation operation = RetriableOperation.None, DocumentMap mapping = null, TimeSpan? commandTimeout = null, CommandBehavior commandBehavior = CommandBehavior.Default)
        {
            Mapping = mapping;
            Statement = statement;
            ParameterValues = parameterValues;
            Operation = operation;
            CommandTimeout = commandTimeout;
            CommandBehavior = commandBehavior;
        }
        
        public string Statement { get; }
        public DocumentMap Mapping { get; }
        public CommandParameterValues ParameterValues { get; }
        public RetriableOperation Operation { get; }
        public TimeSpan? CommandTimeout { get; }
        public CommandBehavior CommandBehavior { get; }
    }
}