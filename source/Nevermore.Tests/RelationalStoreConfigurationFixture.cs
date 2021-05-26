using FluentAssertions;
using NUnit.Framework;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;

namespace Nevermore.Tests
{
    public class RelationalStoreConfigurationFixture
    {
        [Test]
        public void ShouldSetDefaultConnectionStringOptions()
        {
            var config = new RelationalStoreConfiguration("Server=(local);") {ApplicationName = "Nevermore test"};
            var connectionStringBuilder = new SqlConnectionStringBuilder(config.ConnectionString);

            connectionStringBuilder.ConnectTimeout.Should().Be(NevermoreDefaults.DefaultConnectTimeoutSeconds);
            connectionStringBuilder.ConnectRetryCount.Should().Be(NevermoreDefaults.DefaultConnectRetryCount);
            connectionStringBuilder.ConnectRetryInterval.Should().Be(NevermoreDefaults.DefaultConnectRetryInterval);
            connectionStringBuilder.TrustServerCertificate.Should().Be(NevermoreDefaults.DefaultTrustServerCertificate);
        }

        [Test]
        public void ShouldNotOverrideExplicitConnectionStringOptions()
        {
            var config =
                new RelationalStoreConfiguration("Server=(local);Connection Timeout=123;ConnectRetryCount=123;ConnectRetryInterval=59;Trust Server Certificate=False")
                {
                    ApplicationName = "Nevermore test"
                };

            var connectionStringBuilder = new SqlConnectionStringBuilder(config.ConnectionString);

            connectionStringBuilder.ConnectTimeout.Should().Be(123);
            connectionStringBuilder.ConnectRetryCount.Should().Be(123);
            connectionStringBuilder.ConnectRetryInterval.Should().Be(59);
            connectionStringBuilder.TrustServerCertificate.Should().BeFalse();
        }
    }
}