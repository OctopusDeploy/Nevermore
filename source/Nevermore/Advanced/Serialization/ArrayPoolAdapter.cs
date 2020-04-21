using System.Buffers;
using Newtonsoft.Json;

namespace Nevermore.Advanced.Serialization
{
    internal class ArrayPoolAdapter : IArrayPool<char>
    {
        public char[] Rent(int minimumLength)
        {
            return ArrayPool<char>.Shared.Rent(minimumLength);
        }

        public void Return(char[] array)
        {
            ArrayPool<char>.Shared.Return(array);
        }
    }
}