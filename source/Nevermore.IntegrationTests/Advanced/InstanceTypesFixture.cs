using System;
using BenchmarkDotNet.Disassemblers;
using FluentAssertions;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class InstanceTypesFixture : FixtureWithRelationalStore
    {
        abstract class Account
        {
            // This property doesn't need to exist on the document, but you do at least need a column in the result set
            // called `Type` which is a string. Alternatively, you can define it here and map it. If you map it, it 
            // doesn't have to be called Type, and you can use enums or other types to manage it. 
            public string Id { get; set; }
            
            public string Name { get; set; }
            
            public abstract string Type { get; }
        }

        class AzureAccount : Account
        {
            public string AzureSubscriptionId { get; set; }
            public override string Type => "Azure";
        }

        class AwsAccount : Account
        {
            public string SecretKey { get; set; }
            public override string Type => "AWS";
        }

        class AccountMap : DocumentMap<Account>
        {
            public AccountMap()
            {
                // You could define the property with a different name, and just map it to a column called Type
                // if you want
                Column(a => a.Name);
                TypeResolutionColumn(a => a.Type).SaveOnly();
            }
        }

        class AwsAccountTypeResolver : IInstanceTypeResolver
        {
            public Type Resolve(Type baseType, object typeColumnValue)
            {
                if (!typeof(Account).IsAssignableFrom(baseType))
                    return null;

                if ((string) typeColumnValue == "AWS")
                    return typeof(AwsAccount);

                return null;
            }
        }

        class AzureAccountTypeResolver : IInstanceTypeResolver
        {
            public Type Resolve(Type baseType, object typeColumnValue)
            {
                if (!typeof(Account).IsAssignableFrom(baseType))
                    return null;

                if ((string) typeColumnValue == "Azure")
                    return typeof(AzureAccount);
                
                return null;
            }
        }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            
            KeepDataBetweenTests();
            
            Store.Configuration.DocumentMaps.Register(new AccountMap());
            Store.Configuration.InstanceTypeResolvers.Register(new AwsAccountTypeResolver());
            Store.Configuration.InstanceTypeResolvers.Register(new AzureAccountTypeResolver());
            
            ExecuteSql("create table Account (Id nvarchar(200), Name nvarchar(200), Type nvarchar(50), [JSON] nvarchar(max))");
        }

        [Test, Order(1)]
        public void ShouldStoreConcreteAccounts()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Insert(new AwsAccount { Name = "My AWS account", SecretKey = "keys9812"});
            transaction.Insert(new AzureAccount { Name = "My Azure account", AzureSubscriptionId = "sub128721"});
            transaction.Commit();
        }
        
        [Test, Order(2)]
        public void ShouldLoadConcreteTypes()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Load<AwsAccount>("Accounts-1").SecretKey.Should().Be("keys9812");
            transaction.Load<AzureAccount>("Accounts-2").AzureSubscriptionId.Should().Be("sub128721");
        }

        [Test, Order(3)]
        public void QueriesAgainstBaseReturnConcreteTypes()
        {
            using var transaction = Store.BeginTransaction();
            var accounts = transaction.Query<Account>().ToList();
            accounts.Count.Should().Be(2);
            accounts[0].Should().BeOfType<AwsAccount>().Which.SecretKey.Should().Be("keys9812");
            accounts[1].Should().BeOfType<AzureAccount>().Which.AzureSubscriptionId.Should().Be("sub128721");
        }

        [Test, Order(4)]
        public void QueriesAgainstConcreteTypesReturnsOnlyThoseTypes()
        {
            using var transaction = Store.BeginTransaction();
            
            // You can do this, but it will actually read all `Account` objects, then discard those that don't match 
            // the type. So we pay the cost to fetch and deserialize them just to ignore them.
            var accounts = transaction.Query<AwsAccount>().ToList();
            accounts.Count.Should().Be(1);
            accounts[0].Should().BeOfType<AwsAccount>().Which.Name.Should().Be("My AWS account");

            // A faster way is to query against the type
            accounts = transaction.Query<AwsAccount>().Where(a => a.Type == "AWS").ToList();
            accounts.Count.Should().Be(1);
        }

        [Test, Order(5)]
        public void ReturnsNullWhenLoadedAsWrongType()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Load<AwsAccount>("Accounts-1").Should().NotBeNull();
            transaction.Load<AzureAccount>("Accounts-1").Should().BeNull();
            transaction.Load<Account>("Accounts-1").Should().BeOfType<AwsAccount>();
        }

        [Test, Order(5)]
        public void ThrowsWhenUnexpectedTypeIsEncountedByDefault()
        {
            using var transaction = Store.BeginTransaction();
            transaction.ExecuteNonQuery("update Account set [Type] = 'dunno' where Id = 'Accounts-1'");

            Assert.Throws<ReaderException>(() => transaction.Query<Account>().ToList()).Message.Should().StartWith("Error reading row 1, column 2. The 'Type' column has a value of 'dunno' (String)");
        }

        [Test, Order(6)]
        public void ThrowsWhenNullTypeIfBaseIsAbstract()
        {
            using var transaction = Store.BeginTransaction();
            transaction.ExecuteNonQuery("update Account set [Type] = null where Id = 'Accounts-1'");

            Assert.Throws<ReaderException>(() => transaction.Query<Account>().ToList()).Message.Should().StartWith("Error reading row 1, column 3. Could not create an instance of type Nevermore.IntegrationTests.Advanced.InstanceTypesFixture+Account. Type is an interface or abstract class and cannot be instantiated");
        }

        // Rather than throwing, you can add a "catch all" type resolver with a high Order number (this actually runs first)
        class UnknownAccount : Account
        {
            public override string Type => "?";
        }

        class UnknownAccountTypeResolver : IInstanceTypeResolver
        {
            // Runs after all other type handlers
            public int Order => int.MaxValue;
            
            public Type Resolve(Type baseType, object typeColumnValue)
            {
                if (!typeof(Account).IsAssignableFrom(baseType))
                    return null;

                return typeof(UnknownAccount);
            }
        }
        
        [Test, Order(7)]
        public void CanGracefullyHandleUnknownTypes()
        {
            Store.Configuration.InstanceTypeResolvers.Register(new UnknownAccountTypeResolver());
            
            using var transaction = Store.BeginTransaction();
            transaction.ExecuteNonQuery("update Account set [Type] = 'dunno' where Id = 'Accounts-1'");

            var accounts = transaction.Query<Account>().ToList();
            accounts[0].Should().BeOfType<UnknownAccount>().Which.Name.Should().Be("My AWS account");
        }
    }
}