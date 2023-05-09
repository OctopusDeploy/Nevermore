namespace Nevermore.Diagnostics
{
    public interface ITransactionLogger
    {
        void Write(long duration, string transactionName);
    }
}