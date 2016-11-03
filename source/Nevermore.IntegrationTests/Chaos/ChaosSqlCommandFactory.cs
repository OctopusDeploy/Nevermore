using System.Data;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Chaos
{
    public class ChaosSqlCommandFactory : ISqlCommandFactory
    {
        readonly ISqlCommandFactory wrappedFactory;
        readonly double chaosFactor;

        public ChaosSqlCommandFactory(ISqlCommandFactory wrappedFactory, double chaosFactor = 0.2)
        {
            this.wrappedFactory = wrappedFactory;
            this.chaosFactor = chaosFactor;
        }

        public IDbCommand CreateCommand(IDbConnection connection, IDbTransaction transaction, string statement, CommandParameters args, DocumentMap mapping = null)
        {
            return new ChaosSqlCommand(wrappedFactory.CreateCommand(connection, transaction, statement, args), chaosFactor);
        }
    }
}