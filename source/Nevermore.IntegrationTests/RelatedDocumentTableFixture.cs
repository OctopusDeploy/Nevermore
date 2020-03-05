﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using NUnit.Framework;
using TestStack.BDDfy;

namespace Nevermore.IntegrationTests
{
    public class RelatedDocumentTableFixture : FixtureWithRelationalStore
    {
        static readonly IReadOnlyList<RelatedDocumentRow> StartingRecords = new[]
        {
            new RelatedDocumentRow
            {
                Id = "ExistingOrder-1",
                Table = "Order",
                RelatedDocumentId = "Product-1",
                RelatedDocumentType = "Product"
            },
            new RelatedDocumentRow
            {
                Id = "ExistingOrder-1",
                Table = "Order",
                RelatedDocumentId = "Product-2",
                RelatedDocumentType = "Product"
            },
            new RelatedDocumentRow
            {
                Id = "ExistingOrder-2",
                Table = "Order",
                RelatedDocumentId = "Product-1",
                RelatedDocumentType = "Product"
            }
        };

        OrderId orderId;
        Order loadedOrder;

        public void GivenRecordsCurrentlyExist()
        {
            using (var trn = Store.BeginTransaction())
            {
                foreach (var item in StartingRecords)
                    trn.ExecuteNonQuery($"INSERT INTO [{DocumentMap.DefaultRelatedDocumentTableName}] VALUES ('{item.Id}', '{item.Table}', '{item.RelatedDocumentId}', '{item.RelatedDocumentType}')");

                trn.Commit();
            }
        }

        public void AndGivenAnOrderReferencing(string[] referenceIds)
        {
            orderId = new OrderId("Order-1");
            using (var trn = Store.BeginTransaction())
            {
                trn.ExecuteNonQuery($"INSERT INTO [Order] (Id, JSON) VALUES ('{orderId}', '{{}}')");
                foreach (var reference in referenceIds)
                    trn.ExecuteNonQuery($"INSERT INTO [{DocumentMap.DefaultRelatedDocumentTableName}] VALUES ('{orderId}', 'Order', '{reference}', 'Product')");

                trn.Commit();
            }

            // Check that went well
            GetReferencesFromDb().Should().Contain(r => r.Id == orderId && r.RelatedDocumentId == referenceIds[0]);
        }

        public void WhenTheOrderIsRead()
        {
            using (var trn = Store.BeginTransaction())
                loadedOrder = trn.Load<Order, OrderId>(orderId);
        }

        public void WhenANewOrderIsInsertedReferencing(string[] referenceIds)
        {
            var references = referenceIds?.Select(id => (id, typeof(Product)));
            var order = new Order(references);
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
            var references = referenceIds?.Select(id => (id, typeof(Product)));
            var order = new Order(references) {Id = orderId};
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
            var expected = referenceIds.Select(r => new RelatedDocumentRow()
            {
                Id = orderId,
                Table = "Order",
                RelatedDocumentId = r,
                RelatedDocumentType = "Product"
            });
            references.Should().Contain(expected);
        }

        public void AndThenThereAreNoReferencesForThatOrder()
            => AndThenThereAreNoReferencesForThatOrderOtherThan(new string[0]);

        public void AndThenThereAreNoReferencesForThatOrderOtherThan(string[] referenceIds)
        {
            var references = GetReferencesFromDb();
            references
                .Where(r => r.Id != orderId && !referenceIds.Contains(r.RelatedDocumentId))
                .Should()
                .NotContain(r => r.Id == orderId);
        }

        public void AndThenTheOtherReferencesWereNotChanged()
        {
            var references = GetReferencesFromDb();
            references.Should().Contain(StartingRecords);
        }

        public void ThenTheLoadedOrderIsNotNull()
            => loadedOrder.Should().NotBeNull();

#pragma warning restore xUnit1013

        [Test]
        public void Read()
        {
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(new[] {"Product-1", "Product-2"}))
                .When(_ => _.WhenTheOrderIsRead())
                .Then(_ => _.ThenTheLoadedOrderIsNotNull())
                .BDDfy();
        }

        [Test]
        public void Insert()
        {
            var references = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .When(_ => _.WhenANewOrderIsInsertedReferencing(references))
                .Then(_ => _.ThenTheTableContainsTheNewReferencesTo(references))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrderOtherThan(references))
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void InsertNullReferences()
        {
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .When(_ => _.WhenANewOrderIsInsertedReferencing(null))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void Update()
        {
            var starting = new[] {"Product-1", "Product-2"};
            var updated = new[] {"Product-2", "Product-3"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(starting))
                .When(_ => _.WhenTheOrderIsUpdatedReferencing(updated))
                .Then(_ => _.ThenTheTableContainsTheNewReferencesTo(updated))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrderOtherThan(updated))
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void UpdateNullReferences()
        {
            var starting = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(starting))
                .When(_ => _.WhenTheOrderIsUpdatedReferencing(null))
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void Delete()
        {
            var references = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(references))
                .When(_ => _.WhenTheOrderIsDeleted())
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        [Test]
        public void DeleteWithQueryBuilder()
        {
            var references = new[] {"Product-1", "Product-2"};
            this.Given(_ => _.GivenRecordsCurrentlyExist())
                .And(_ => _.AndGivenAnOrderReferencing(references))
                .When(_ => _.WhenTheOrderIsDeletedUsingTheQueryBuilder())
                .And(_ => _.AndThenThereAreNoReferencesForThatOrder())
                .And(_ => _.AndThenTheOtherReferencesWereNotChanged())
                .BDDfy();
        }

        RelatedDocumentRow[] GetReferencesFromDb()
        {
            var map = new OrderMap().RelatedDocumentsMappings.First();

            Func<IDataReader, RelatedDocumentRow> Callback()
                => reader
                    => new RelatedDocumentRow
                    {
                        Id = (string) reader[map.IdColumnName],
                        Table = (string) reader[map.IdTableColumnName],
                        RelatedDocumentId = (string) reader[map.RelatedDocumentIdColumnName],
                        RelatedDocumentType = (string) reader[map.RelatedDocumentTableColumnName],
                    };

            using (var trn = Store.BeginTransaction())
            {
                return trn.ExecuteReaderWithProjection(
                        $"SELECT * FROM [{DocumentMap.DefaultRelatedDocumentTableName}]",
                        new CommandParameterValues(),
                        m => m.Read(Callback())
                    )
                    .ToArray();
            }
        }

        public class RelatedDocumentRow : IEquatable<RelatedDocumentRow>
        {
            public string Id { get; set; }
            public string Table { get; set; }
            public string RelatedDocumentId { get; set; }
            public string RelatedDocumentType { get; set; }

            public bool Equals(RelatedDocumentRow other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                var eq = string.Equals(Id, other.Id) && string.Equals(Table, other.Table) && string.Equals(RelatedDocumentId, other.RelatedDocumentId) && string.Equals(RelatedDocumentType, other.RelatedDocumentType);
                return eq;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((RelatedDocumentRow) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Id.GetHashCode();
                    hashCode = (hashCode * 397) ^ Table.GetHashCode();
                    hashCode = (hashCode * 397) ^ RelatedDocumentId.GetHashCode();
                    hashCode = (hashCode * 397) ^ RelatedDocumentType.GetHashCode();
                    return hashCode;
                }
            }
            
            public static bool operator ==(RelatedDocumentRow left, RelatedDocumentRow right) => Equals(left, right);
            public static bool operator !=(RelatedDocumentRow left, RelatedDocumentRow right) => !Equals(left, right);
        }
    }
}