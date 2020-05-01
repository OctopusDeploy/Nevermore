using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    [TestFixture]
    public class SerializationBinderFixture : FixtureWithRelationalStore 
    {
        class OrderHistory
        {
            public string Id { get; set; }
            public string OrderId { get; set; }
            public List<IAuditEvent> AuditEvents { get; } = new List<IAuditEvent>(); 
        }

        interface IAuditEvent  { }
        class CreatedEvent : IAuditEvent  { }
        class EditedEvent : IAuditEvent  { }

        class OrderMap : DocumentMap<OrderHistory>
        {
            public OrderMap()
            {
                Column(o => o.OrderId);
            }
        }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            NoMonkeyBusiness();
            Configuration.DocumentMaps.Register(new OrderMap());
            ExecuteSql("create table TestSchema.OrderHistory(Id nvarchar(200) not null, OrderId nvarchar(200) not null, [JSON] nvarchar(max) not null)");
        }

        [Test, Order(1)]
        public void NormallyPutsFullType()
        {
            var orderHistory = new OrderHistory();
            orderHistory.OrderId = "Order-123";
            orderHistory.AuditEvents.Add(new CreatedEvent());
            orderHistory.AuditEvents.Add(new EditedEvent());
            orderHistory.AuditEvents.Add(new EditedEvent());

            using var transaction = Store.BeginTransaction();
            transaction.Insert(orderHistory);

            var json = transaction.Stream<string>("select top 1 JSON from TestSchema.OrderHistory").Single();
            var token = JToken.Parse(json);
            var events = token.Value<JArray>("AuditEvents");
            events.Count.Should().Be(3);
            
            // This helps us to deserialize the right type, but it's messy!
            events.Value<JObject>(0).Value<string>("$type").Should().Be("Nevermore.IntegrationTests.Advanced.SerializationBinderFixture+CreatedEvent, Nevermore.IntegrationTests");
            events.Value<JObject>(1).Value<string>("$type").Should().Be("Nevermore.IntegrationTests.Advanced.SerializationBinderFixture+EditedEvent, Nevermore.IntegrationTests");
            events.Value<JObject>(2).Value<string>("$type").Should().Be("Nevermore.IntegrationTests.Advanced.SerializationBinderFixture+EditedEvent, Nevermore.IntegrationTests");
            
            
            Console.WriteLine(token.ToString());
        }

        class MySerializationBinder : ISerializationBinder
        {
            readonly ISerializationBinder fallback;

            public MySerializationBinder(ISerializationBinder fallback)
            {
                this.fallback = fallback ?? new DefaultSerializationBinder();
            }
            
            public Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == "Created" && assemblyName == null)
                    return typeof(CreatedEvent);
                if (typeName == "Edited" && assemblyName == null)
                    return typeof(EditedEvent);
                return fallback.BindToType(assemblyName, typeName);
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                if (serializedType == typeof(CreatedEvent))
                {
                    typeName = "Created";
                    assemblyName = null;
                    return;
                }
                if (serializedType == typeof(EditedEvent))
                {
                    typeName = "Edited";
                    assemblyName = null;
                    return;
                }

                fallback.BindToName(serializedType, out assemblyName, out typeName);
            }
        }
        

        [Test, Order(2)]
        public void PutsOurCustomTypeInstead()
        {
            Configuration.UseJsonNetSerialization(settings =>
            {
                settings.SerializationBinder = new MySerializationBinder(settings.SerializationBinder);
            });
            
            var orderHistory = new OrderHistory();
            orderHistory.OrderId = "Order-123";
            orderHistory.AuditEvents.Add(new CreatedEvent());
            orderHistory.AuditEvents.Add(new EditedEvent());
            orderHistory.AuditEvents.Add(new EditedEvent());

            using var transaction = Store.BeginTransaction();
            transaction.Insert(orderHistory);

            var json = transaction.Stream<string>("select top 1 JSON from TestSchema.OrderHistory").Single();
            var token = JToken.Parse(json);
            var events = token.Value<JArray>("AuditEvents");
            events.Count.Should().Be(3);
            
            // This helps us to deserialize the right type, but it's messy!
            events.Value<JObject>(0).Value<string>("$type").Should().Be("Created");
            events.Value<JObject>(1).Value<string>("$type").Should().Be("Edited");
            events.Value<JObject>(2).Value<string>("$type").Should().Be("Edited");

            var loaded = transaction.Load<OrderHistory>("OrderHistorys-1");
            loaded.AuditEvents[0].Should().BeOfType<CreatedEvent>();
            loaded.AuditEvents[1].Should().BeOfType<EditedEvent>();
            loaded.AuditEvents[2].Should().BeOfType<EditedEvent>();
        }
    }
}