#nullable enable
using System.Text;

namespace Nevermore
{
    public interface ITransactionDiagnostic
    {
        public string? Name { get; }
        public void WriteCurrentTransactions(StringBuilder output);
    }
}