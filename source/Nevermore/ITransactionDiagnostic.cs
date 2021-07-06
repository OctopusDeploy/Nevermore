#nullable enable
using System.Text;

namespace Nevermore
{
    internal interface ITransactionDiagnostic
    {
        public string? Name { get; }
        public void WriteCurrentTransactions(StringBuilder output);
    }
}