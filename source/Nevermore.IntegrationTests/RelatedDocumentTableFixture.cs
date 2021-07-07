using System.Collections.Generic;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;
using TestStack.BDDfy;

namespace Nevermore.IntegrationTests
{
    public class RelatedDocumentTableFixture : FixtureWithRelationalStore
    {
        readonly RelatedDocumentBdd relatedDocumentBdd;

        public RelatedDocumentTableFixture()
        {
            relatedDocumentBdd = new RelatedDocumentBdd(Store);
        }

#pragma warning restore xUnit1013

        [Test]
        public void Read()
        {
            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .And(_ => _.relatedDocumentBdd.AndGivenAnOrderReferencing(new[] {"Product-1", "Product-2"}))
                .When(_ => _.relatedDocumentBdd.WhenTheOrderIsRead())
                .Then(_ => _.relatedDocumentBdd.ThenTheLoadedOrderIsNotNull())
                .BDDfy();
        }

        [TestCase(1)]
        [TestCase(1000)]
        [TestCase(2001)]
        [TestCase(3001)] // exceeds the per command param limit
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
        public void InsertNullReferences()
        {
            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .When(_ => _.relatedDocumentBdd.WhenANewOrderIsInsertedReferencing(null))
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [TestCase(1)]
        [TestCase(1000)]
        [TestCase(2001)]
        [TestCase(3001)] // exceeds the per command param limit
        public void Update(int referenceDataEntriesCount)
        {
            var startingData = new List<string>();
            var updatedData = new List<string>();
            for (int i = 0; i < referenceDataEntriesCount; i++)
            {
                trn.Delete(order);
                trn.Commit();
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
        public void UpdateNullReferences()
        {
            var starting = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .And(_ => _.relatedDocumentBdd.AndGivenAnOrderReferencing(starting))
                .When(_ => _.relatedDocumentBdd.WhenTheOrderIsUpdatedReferencing(null))
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void Delete()
        {
            var references = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .And(_ => _.relatedDocumentBdd.AndGivenAnOrderReferencing(references))
                .When(_ => _.relatedDocumentBdd.WhenTheOrderIsDeleted())
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void DeleteWithQueryBuilder()
        {
            var references = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.relatedDocumentBdd.GivenRecordsCurrentlyExist())
                .And(_ => _.relatedDocumentBdd.AndGivenAnOrderReferencing(references))
                .When(_ => _.relatedDocumentBdd.WhenTheOrderIsDeletedUsingTheQueryBuilder())
                .And(_ => _.relatedDocumentBdd.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.relatedDocumentBdd.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }
    }
}
