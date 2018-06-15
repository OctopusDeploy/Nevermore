using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using TestStack.BDDfy;
using TestStack.BDDfy.Xunit;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Nevermore.IntegrationTests
{
#pragma warning disable xUnit1013 
    public class RelatedDocumentsTableFixture : FixtureWithRelationalStore
    {
        static readonly IReadOnlyList<(string id, string referencedId)> startingRecords = new[]
        {
            ("Parent-1", "Child-1"),
            ("Parent-1", "Child-2"),
            ("Parent-2", "Child-1"),
        };

        string orderId;
        Order loadedOrder;

        public RelatedDocumentsTableFixture(ITestOutputHelper output) : base(output)
        {
        }


        public void GivenRecordsCurrentlyExist()
        {
            using (var trn = Store.BeginTransaction())
            {
                foreach (var item in startingRecords)
                    trn.ExecuteNonQuery($"INSERT INTO [{DocumentMap.DefaultRelatedDocumentsTableName}] VALUES ('{item.id}', '{item.referencedId}')");

                trn.Commit();
            }
        }

        public void AndGivenAnOrderReferencing(string[] referenceIds)
        {
            orderId = "Order-1";
            using (var trn = Store.BeginTransaction())
            {
                trn.ExecuteNonQuery($"INSERT INTO [Order] (Id, JSON) VALUES ('{orderId}', '{{}}')");
                foreach (var reference in referenceIds)
                    trn.ExecuteNonQuery($"INSERT INTO [{DocumentMap.DefaultRelatedDocumentsTableName}] VALUES ('{orderId}', '{reference}')");

                trn.Commit();
            }

            // Check that went well
            GetReferencesFromDb().Should().Contain((orderId, referenceIds[0]));
        }

        public void WhenTheOrderIsRead()
        {
            using (var trn = Store.BeginTransaction())
                loadedOrder = trn.Load<Order>(orderId);
        }

        public void WhenANewOrderIsInsertedReferencing(string[] referenceIds)
        {
            var order = new Order(referenceIds);
            using (var trn = Store.BeginTransaction())
            {
                trn.Insert(order);
                trn.Commit();
            }

            Console.WriteLine("New Order ID: " + order.Id);
            orderId = order.Id;
        }

        public void WhenTheOrderIsUpdatedReferencing(string[] referenceIds)
        {
            var order = new Order(referenceIds) {Id = orderId};
            using (var trn = Store.BeginTransaction())
            {
                trn.Update(order);
                trn.Commit();
            }
        }

        public void WhenTheOrderIsDeleted()
        {
            var order = new Order() {Id = orderId};
            using (var trn = Store.BeginTransaction())
            {
                trn.Delete(order);
                trn.Commit();
            }
        }

        public void WhenTheOrderIsDeletedUsingTheQueryBuilder()
        {
            var order = new Order() {Id = orderId};
            using (var trn = Store.BeginTransaction())
            {
                trn.DeleteQuery<Order>()
                    .Where(nameof(order.Id), UnarySqlOperand.Equal, orderId)
                    .Delete();
                trn.Commit();
            }
        }

        public void ThenTheTableContainsTheNewReferencesTo(string[] referenceIds)
        {
            var references = GetReferencesFromDb();
            var expected = referenceIds.Select(r => (orderId, r));
            references.Should().Contain(expected);
        }

        public void AndThenThereAreNoReferencesForThatOrder()
            => AndThenThereAreNoReferencesForThatOrderOtherThan(new string[0]);

        public void AndThenThereAreNoReferencesForThatOrderOtherThan(string[] referenceIds)
        {
            var references = GetReferencesFromDb();
            var expected = referenceIds.Select(r => (id: orderId, referencedId: r));
            references.Except(expected)
                .Should()
                .NotContain(r => r.id == orderId);
        }

        public void AndThenTheOtherReferencesWereNotChanged()
        {
            var references = GetReferencesFromDb();
            references.Should().Contain(startingRecords);
        }

        public void ThenTheLoadedOrderIsNotNull()
            => loadedOrder.Should().NotBeNull();

#pragma warning restore xUnit1013
        
        [BddfyFact]
        public void Read()
        {
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(new[] {"Other-1", "Other-2"}))
                .When(_ => _.WhenTheOrderIsRead())
                .Then(_ => _.ThenTheLoadedOrderIsNotNull())
                .BDDfy();
        }

        [BddfyFact]
        public void Insert()
        {
            var references = new[] {"Other-1", "Other-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .When(_ => _.WhenANewOrderIsInsertedReferencing(references))
                .Then(_ => _.ThenTheTableContainsTheNewReferencesTo(references))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrderOtherThan(references))
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [BddfyFact]
        public void InsertNullReferences()
        {
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .When(_ => _.WhenANewOrderIsInsertedReferencing(null))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [BddfyFact]
        public void Update()
        {
            var starting = new[] {"Other-1", "Other-2"};
            var updated = new[] {"Other-2", "Other-3"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(starting))
                .When(_ => _.WhenTheOrderIsUpdatedReferencing(updated))
                .Then(_ => _.ThenTheTableContainsTheNewReferencesTo(updated))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrderOtherThan(updated))
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [BddfyFact]
        public void UpdateNullReferences()
        {
            var starting = new[] {"Other-1", "Other-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(starting))
                .When(_ => _.WhenTheOrderIsUpdatedReferencing(null))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [BddfyFact]
        public void Delete()
        {
            var references = new[] {"Other-1", "Other-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(references))
                .When(_ => _.WhenTheOrderIsDeleted())
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }
        
        [BddfyFact]
        public void DeleteWithQueryBuilder()
        {
            var references = new[] {"Other-1", "Other-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(references))
                .When(_ => _.WhenTheOrderIsDeletedUsingTheQueryBuilder())
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        (string id, string referencedId)[] GetReferencesFromDb()
        {
            var map = new OrderMap().RelatedDocumentsMappings.First();

            Func<IDataReader, (string id, string referencedId)> Callback()
                => reader
                    => ((string) reader[map.IdColumnName], (string) reader[map.RelatedDocumentIdColumnName]);

            using (var trn = Store.BeginTransaction())
            {
                return trn.ExecuteReaderWithProjection<(string id, string referencedId)>(
                        $"SELECT * FROM [{DocumentMap.DefaultRelatedDocumentsTableName}]",
                        new CommandParameterValues(),
                        m => m.Read(Callback())
                    )
                    .ToArray();
            }
        }
    }
}