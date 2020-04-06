#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests
{
    public class RelationalStoreFixture
    {
        [Test]
        public void ShouldSetDefaultConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            connectionStringBuilder.ConnectTimeout.Should().Be(NevermoreDefaults.DefaultConnectTimeoutSeconds);
            connectionStringBuilder.ConnectRetryCount.Should().Be(NevermoreDefaults.DefaultConnectRetryCount);
            connectionStringBuilder.ConnectRetryInterval.Should().Be(NevermoreDefaults.DefaultConnectRetryInterval);
        }

        [Test]
        public void ShouldNotOverrideExplicitConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);Connection Timeout=123;ConnectRetryCount=123;ConnectRetryInterval=59;", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            connectionStringBuilder.ConnectTimeout.Should().Be(123);
            connectionStringBuilder.ConnectRetryCount.Should().Be(123);
            connectionStringBuilder.ConnectRetryInterval.Should().Be(59);
        }
    }
}