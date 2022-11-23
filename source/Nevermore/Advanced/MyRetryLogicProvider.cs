// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Data.SqlClient;
//
// namespace Nevermore.Advanced
// {
//     internal class MyRetryLogicProvider : SqlRetryLogicBaseProvider
//     {
//         public override TResult Execute<TResult>(object sender, Func<TResult> function)
//         {
//             for (var i = 0; i < 5; i++)
//             {
//                 try
//                 {
//                     return function.Invoke();
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                     // throw;
//                 }
//             }
//
//             throw new AggregateException();
//         }
//
//         public override async Task<TResult> ExecuteAsync<TResult>(object sender, Func<Task<TResult>> function, CancellationToken cancellationToken = new CancellationToken())
//         {
//             for (var i = 0; i < 5; i++)
//             {
//                 try
//                 {
//                     return await function.Invoke();
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                     // throw;
//                 }
//             }
//
//             throw new AggregateException();
//         }
//
//         public override async Task ExecuteAsync(object sender, Func<Task> function, CancellationToken cancellationToken = new CancellationToken())
//         {
//             for (var i = 0; i < 5; i++)
//             {
//                 try
//                 {
//                     await function.Invoke();
//                 }
//                 catch (Exception e)
//                 {
//                     Console.WriteLine(e);
//                     // throw;
//                 }
//             }
//
//             throw new AggregateException();
//         }
//     }
// }