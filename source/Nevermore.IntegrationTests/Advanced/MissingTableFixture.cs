using System;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class MissingTableFixture : FixtureWithRelationalStore
    {
        class Missing
        {
            public string Id { get; set; }
        }

        class MissingMap : DocumentMap<Missing>
        {
            public MissingMap()
            {
                Id(u => u.Id);
            }
        }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            NoMonkeyBusiness();
            Configuration.DocumentMaps.Register(new MissingMap());
        }

        [Test]
        public void QueryShouldThrowAUsefulException()
        {
            using var transaction = Store.BeginTransaction();

            void Run()
                => transaction.Query<Missing>().ToArray();

            ((Action)Run).Should().Throw<Exception>()
                .WithMessage("No columns found for table or view 'TestSchema.Missing'. The table or view likely does not exist in that schema.");
        }
    }
}