using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced.Queryable
{
    internal static class AsyncEnumerableAdapter
    {
        public static async Task<IEnumerable> ConvertToEnumerable(object asyncEnumerable, Type sequenceType, CancellationToken cancellationToken)
        {
            var asyncEnumerableType = typeof(IAsyncEnumerable<>).MakeGenericType(sequenceType);
            var asyncEnumeratorType = typeof(IAsyncEnumerator<>).MakeGenericType(sequenceType);

            var getAsyncEnumeratorMethod = asyncEnumerableType.GetRuntimeMethod("GetAsyncEnumerator", new[] { typeof(CancellationToken) });
            var moveNextAsyncMethod = asyncEnumeratorType.GetRuntimeMethod("MoveNextAsync", Array.Empty<Type>());
            var currentProperty = asyncEnumeratorType.GetRuntimeProperty("Current");

            var asyncEnumerator = getAsyncEnumeratorMethod.Invoke(asyncEnumerable, new object[] { cancellationToken });

            var list = CreateList(sequenceType);
            while (await ((ValueTask<bool>)moveNextAsyncMethod.Invoke(asyncEnumerator, Array.Empty<object>())).ConfigureAwait(false))
            {
                var item = currentProperty.GetValue(asyncEnumerator);
                list.Add(item);
            }

            return list;
        }

        static IList CreateList(Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType);
            return list;
        }
    }
}