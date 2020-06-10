namespace Nevermore.Diagnostics
{
    public interface IQueryLogger
    {
        void Insert(long duration, string transactionName, string statement);
        void Update(long duration, string transactionName, string statement);
        void Delete(long duration, string transactionName, string statement);
        void NonQuery(long duration, string transactionName, string statement);
        void ProcessReader(long duration, string transactionName, string statement);
        void ExecuteReader(long duration, string transactionName, string statement);
        void Scalar(long duration, string transactionName, string statement);
    }
}
