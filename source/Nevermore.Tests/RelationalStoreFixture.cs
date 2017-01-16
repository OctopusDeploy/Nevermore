using System.Data.SqlClient;
using NUnit.Framework;

namespace Nevermore.Tests
{
    [TestFixture]
    public class RelationalStoreFixture
    {
        [Test]
        public void ShouldSetDefaultConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            Assert.That(connectionStringBuilder.ConnectTimeout, Is.EqualTo(RelationalStore.DefaultConnectTimeoutSeconds));
            Assert.That(connectionStringBuilder.ConnectRetryCount, Is.EqualTo(RelationalStore.DefaultConnectRetryCount));
            Assert.That(connectionStringBuilder.ConnectRetryInterval, Is.EqualTo(RelationalStore.DefaultConnectRetryInterval));
        }

        [Test]
        public void ShouldNotOverrideExplicitConnectionStringOptions()
        {
            var store = new RelationalStore("Server=(local);Connection Timeout=123;ConnectRetryCount=123;ConnectRetryInterval=59;", "Nevermore test", null, null, null, null, 20);

            var connectionStringBuilder = new SqlConnectionStringBuilder(store.ConnectionString);

            Assert.That(connectionStringBuilder.ConnectTimeout, Is.EqualTo(123));
            Assert.That(connectionStringBuilder.ConnectRetryCount, Is.EqualTo(123));
            Assert.That(connectionStringBuilder.ConnectRetryInterval, Is.EqualTo(59));
        }
    }
}