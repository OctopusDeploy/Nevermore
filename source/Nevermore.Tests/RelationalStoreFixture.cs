using System.Data.SqlClient;
using Xunit;

namespace Nevermore.Tests
{
    public class RelationalStoreFixture
    {
        [Fact]
        public void ShouldSetDefaultConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            Assert.Equal(RelationalStore.DefaultConnectTimeoutSeconds, connectionStringBuilder.ConnectTimeout);
            Assert.Equal(RelationalStore.DefaultConnectRetryCount, connectionStringBuilder.ConnectRetryCount);
            Assert.Equal(RelationalStore.DefaultConnectRetryInterval, connectionStringBuilder.ConnectRetryInterval);
        }

        [Fact]
        public void ShouldNotOverrideExplicitConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);Connection Timeout=123;ConnectRetryCount=123;ConnectRetryInterval=59;", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            Assert.Equal(123, connectionStringBuilder.ConnectTimeout);
            Assert.Equal(123, connectionStringBuilder.ConnectRetryCount);
            Assert.Equal(59, connectionStringBuilder.ConnectRetryInterval);
        }
    }
}