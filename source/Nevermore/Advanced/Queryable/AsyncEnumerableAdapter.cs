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
        static readonly MethodInfo HelperMethod = typeof(AsyncEnumerableAdapter).GetMethod(nameof(Helper), BindingFlags.Static | BindingFlags.NonPublic);

        public static Task<IEnumerable> ConvertToEnumerable(object asyncEnumerable, Type sequenceType, CancellationToken cancellationToken)
        {
            return (Task<IEnumerable>)HelperMethod.MakeGenericMethod(sequenceType).Invoke(null, new[] {asyncEnumerable, cancellationToken });
        }

        static async Task<IEnumerable> Helper<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            var list = new List<T>();
            await foreach (var element in enumerable.WithCancellation(cancellationToken))
            {
                list.Add(element);
            }
            return list;
        }
    }
}