namespace Nevermore
{
    public interface IOrderedQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        IOrderedQueryBuilder<TRecord> ThenBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> ThenByDescending(string orderByClause);
    }
}
