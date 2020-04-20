using System;

namespace Nevermore.Advanced.ReaderStrategies.Compilation
{
    internal class CompiledExpression<TExpression>
    {
        readonly Lazy<string> expressionSource;

        public CompiledExpression(TExpression execute, Lazy<string> expressionSource)
        {
            Execute = execute;
            this.expressionSource = expressionSource;
        }
        
        public TExpression Execute;
        public string ExpressionSource => expressionSource.Value;
    }
}