using System;
using System.Collections.Generic;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;
using TestStack.BDDfy;

namespace Nevermore.IntegrationTests
{
    public class RelatedDocumentTableNoSplittingFixture : FixtureWithRelationalStore
    {
        readonly RelatedDocumentBdd relatedDocumentBdd;
        const int SqlCommandParameterLimit = 2100;

        public RelatedDocumentTableNoSplittingFixture()
        {
            Store.Configuration.EnableSplittingCommands = false;
            relatedDocumentBdd = new RelatedDocumentBdd(Store);
        }

        [TestCase(1)]
        [TestCase(1000)]
        [TestCase(2001)]
        public void Insert(int referenceDataEntriesCount)
        {
            var referenceData = new List<string>();
            for (int i = 0; i < referenceDataEntriesCount; i++)
            {
                referenceData.Add("Product-" + i);
            }
            var references = referenceData.ToArray();

            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .When(_ => _.relatedDocumentBdd.WhenANewOrderIsInsertedReferencing(references))
                .Then(_ => _.relatedDocumentBdd.ThenTheTableContainsTheNewReferencesTo(references))
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrderOtherThan(references))
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void FailInsertWhenSplittingCommandsDisallowed()
        {
            var referenceData = new List<string>();
            for (int i = 0; i < SqlCommandParameterLimit; i++)
            {
                referenceData.Add("Product-" + i);
            }

            var references = referenceData.ToArray();

            relatedDocumentBdd.GivenRecordsCurrentlyExist();
            Assert.Throws<InvalidOperationException>(() => relatedDocumentBdd.WhenANewOrderIsInsertedReferencing(references));
        }

        [TestCase(1)]
        [TestCase(1000)]
        [TestCase(2001)]
        public void Update(int referenceDataEntriesCount)
        {
            var startingData = new List<string>();
            var updatedData = new List<string>();
            for (int i = 0; i < referenceDataEntriesCount; i++)
            {
                startingData.Add("Product-" + i);
                updatedData.Add("Product-" + i + 1);
            }
            var starting = startingData.ToArray();
            var updated = updatedData.ToArray();

            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .And(_ => _.relatedDocumentBdd.AndGivenAnOrderReferencing(starting))
                .When(_ => _.relatedDocumentBdd.WhenTheOrderIsUpdatedReferencing(updated))
                .Then(_ => _.relatedDocumentBdd.ThenTheTableContainsTheNewReferencesTo(updated))
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrderOtherThan(updated))
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void FailUpdateWhenSplittingCommandsDisallowed()
        {
            var startingData = new List<string>();
            var updatedData = new List<string>();
            for (int i = 0; i < SqlCommandParameterLimit; i++)
            {
                startingData.Add("Product-" + i);
                updatedData.Add("Product-" + i + 1);
            }
            var starting = startingData.ToArray();
            var updated = updatedData.ToArray();

            relatedDocumentBdd.GivenRecordsCurrentlyExist();
            relatedDocumentBdd.AndGivenAnOrderReferencing(starting);
            Assert.Throws<InvalidOperationException>(() => relatedDocumentBdd.WhenTheOrderIsUpdatedReferencing(updated));
        }
    }
}