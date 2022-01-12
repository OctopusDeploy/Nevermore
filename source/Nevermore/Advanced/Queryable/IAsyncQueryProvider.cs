using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced.Queryable
{
    public interface IAsyncQueryProvider : IQueryProvider
    {
        public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken);
    }
}