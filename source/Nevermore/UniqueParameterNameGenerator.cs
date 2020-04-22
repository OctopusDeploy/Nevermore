using System.Collections.Generic;

namespace Nevermore
{
    public interface IUniqueParameterNameGenerator
    {
        void Push();
        string GenerateUniqueParameterName(string parameterName);
        void Pop();
    }

    internal class UniqueParameterNameGenerator : IUniqueParameterNameGenerator
    {
        readonly Stack<int> scopeStack = new Stack<int>();
        int parameterCount;
        
        public string GenerateUniqueParameterName(string parameterName)
        {
            return $"{new Parameter(parameterName).ParameterName}_{parameterCount++}";
        }

        public void Push()
        {
            scopeStack.Push(parameterCount);
        }

        public void Pop()
        {
            if (scopeStack.Count > 0)
            {
                parameterCount = scopeStack.Pop();
            }

            parameterCount = 0;
        }
    }
}