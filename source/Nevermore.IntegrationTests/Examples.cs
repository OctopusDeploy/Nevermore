using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class Examples : FixtureWithDatabase
    {
        // Welcome! We hope these examples help you to get acquainted with Nevermore.
        //
        // Nevermore is designed to map documents. Here's an example of a document we'll be working with.
        // The only assumption is that you'll provide an "Id" property which is a string.
        class Person
        {
            public string Id { get; private set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            
            // Documents can have all kinds of things on them, including arrays, nested properties, and more. 
            // All these properties will be stored in the JSON blob.
            public HashSet<string> Tags { get; } = new HashSet<string>();
            
            public int[] LuckyNumbers { get; set; }
        }
        
        // To translate documents to a relational database, we need a mapping. Here's our map. It tells us which
        // properties will be stored as columns on the table (everything else is stored in a blob of JSON at the end of
        // the table).
        class PersonMap : DocumentMap<Person>
        {
            public PersonMap()
            {
                Column(m => m.FirstName).MaxLength(20);
                Column(m => m.LastName).Nullable();
                Column(m => m.Email);
                Unique("UniquePersonEmail", new[] { "Email" }, "People must have unique emails");
            }
        }

        readonly IRelationalStore store;

        public Examples()
        {
            // This is where you configure nevermore. The config lets you control how JSON is serialized and a whole lot
            // more. We'll start simple.
            var config = new RelationalStoreConfiguration(ConnectionString);
            
            // Your mappings define how your documents will be stored in the database. You need to tell Nevermore about
            // all your mappings.
            config.Mappings.Register(new PersonMap());
            
            // Create your store. You'll do this once when the application starts up.
            store = new RelationalStore(config);
            
            // Of course, this is a SQL database, so you'll need a SQL schema. Here's ours. 
            ExecuteSql(@"
                CREATE TABLE [Person] (
                  [Id] NVARCHAR(50) NOT NULL CONSTRAINT [PK__Id] PRIMARY KEY CLUSTERED, 
                  [FirstName] NVARCHAR(20) NOT NULL, 
                  [LastName] NVARCHAR(200) NULL, 
                  [Email] NVARCHAR(200) NOT NULL, 
                  [JSON] NVARCHAR(MAX) NOT NULL
                )
                ALTER TABLE [Person] ADD CONSTRAINT [UQ_UniquePersonEmail] UNIQUE([Email])
                ");
        }

        [Test, Order(1)]
        public void Insert()
        {
            var person = new Person {FirstName = "Donald", LastName = "Duck", Email = "donald.duck@disney.com", Tags = {"duck", "disney", "\u2103"}, LuckyNumbers = Enumerable.Range(0, 85000).ToArray()};
            
            using var transaction = store.BeginTransaction();
            transaction.Insert(person);
            transaction.Commit();
            
            // ID's are assigned automatically when the Insert call completes.
            person.Id.Should().Be("Persons-1");
        }

        [Test, Order(2)]
        public void Load()
        {
            // If you know the document ID you want, you can load it back
            using var transaction = store.BeginTransaction();
            var person = transaction.Load<Person>("Persons-1");
            person.FirstName.Should().Be("Donald");
        }

        [Test, Order(3)]
        public void InsertMany()
        {
            // You can insert many documents, but be careful - this is limited to a few hundred documents at a time. 
            using var transaction = store.BeginTransaction();
            transaction.InsertMany(
                new List<Person>
                {
                    new Person {FirstName = "Daffy", LastName = "Duck", Email = "daffy.duck@wb.com", Tags = {"duck", "wb"}},
                    new Person {FirstName = "Buggs", LastName = "Bunny", Email = "buggs.bunny@wb.com", Tags = {"rabbit", "wb"}},
                    new Person {FirstName = "Prince", LastName = null, Email = "prince", Tags = {"singer"}},
                });
            transaction.Commit();
        }

        [Test, Order(4)]
        public void UniqueConstraintsAreEnforced()
        {
            using var transaction = store.BeginTransaction();
            var ex = Assert.Throws<UniqueConstraintViolationException>(delegate
            {
                // These two people have the same email. 
                // But our document map and table schema set up a unique constraint on this field. The message
                // comes from the document map.
                transaction.InsertMany(
                    new List<Person>
                    {
                        new Person {FirstName = "A", LastName = "A", Email = "same@email.com"},
                        new Person {FirstName = "B", LastName = "B", Email = "same@email.com"}
                    });
            });
            ex.Message.Should().Be("People must have unique emails");
        }
        
        [Test, Order(5)]
        public void Query()
        {
            using var transaction = store.BeginTransaction();
            
            // Beyond "Load", most of the queries you'll write will be against collections of documents. Since 
            // properties that you "map" are stored as columns, you can query against those columns.
            // Here are some different ways to query.
            // TableQuery gives you a strongly typed collection:
            var person = transaction.TableQuery<Person>()
                .Where("FirstName = @name and Email is not null")                // This becomes the SQL "where" clause
                .Parameter("name", "Donald")
                .FirstOrDefault();

            person.LastName.Should().Be("Duck");
            
            // If for some reason you want to query a SQL database but SQL scares you, you can also use LINQ support: 
            person = transaction.TableQuery<Person>()
                .Where(m => m.FirstName == "Donald")
                .FirstOrDefault();

            person.LastName.Should().Be("Duck");
            
            // Or, you can use a perfectly good language for querying SQL, called... SQL!
            // Nevermore handles the mapping of the result set to the object type
            person = transaction.Stream<Person>(
                "select * from dbo.Person where FirstName = @name",
                new CommandParameterValues {{"name", "Donald"}}
                ).Single();

            person.LastName.Should().Be("Duck");
            
            // SQL Server 2016 and above supports JSON_VALUE as a function. This can be used to query for data stored 
            // in the JSON blob at the end of the document.
            // For example, you can use JSON_VALUE to query a single field within the JSON. Or you can use OPENJSON
            // to query values in an array. The only downside to doing this of course is that you won't get to take 
            // much advantage of indexes.
            person = transaction.TableQuery<Person>()
                .Where("exists (SELECT value FROM OPENJSON([JSON],'$.Tags') where value = @tag1) and exists (SELECT value FROM OPENJSON([JSON],'$.Tags') where value = @tag2)")
                .Parameter("tag1", "wb")
                .Parameter("tag2", "duck")
                .FirstOrDefault();
            
            person.FirstName.Should().Be("Daffy");
        }

        [Test, Order(6)]
        public void QueryWithTuples()
        {
            using var transaction = store.BeginTransaction();
            
            // The results of your queries don't always have to be documents. You can just query an arbitrary type if
            // you want. The ID and JSON columns don't need to appear in the result set.
            var customer = transaction.Stream<(string Email, string FirstName)>(
                "select Email, FirstName from dbo.Person where Email = @email", new CommandParameterValues { { "email", "donald.duck@disney.com" } }
            ).Single();

            customer.Email.Should().Be("donald.duck@disney.com");
            
            // This pattern can be used for just about any quick query
            var result = transaction.Stream<(string LastName, int Count)>(
                "select LastName, count(*) from dbo.Person group by LastName order by count(*) desc, len(LastName) desc"
            ).ToList();

            result.Count.Should().Be(3);
            result[0].LastName.Should().Be("Duck");
            result[0].Count.Should().Be(2);
            result[1].LastName.Should().Be("Bunny");
            result[1].Count.Should().Be(1);
            result[2].LastName.Should().BeNull();
            result[2].Count.Should().Be(1);
        }

        [Test, Order(7)]
        public void QueryPrimitives()
        {
            using var transaction = store.BeginTransaction();
            
            // Just need a single column? No problem, you can stream primitives (Strings, numbers, and so on)
            var names = transaction.Stream<string>(
                "select FirstName from dbo.Person order by FirstName"
            ).ToList();

            string.Join(", ", names).Should().Be("Buggs, Daffy, Donald, Prince");
        }

        // You can also query against arbitrary classes, but unless they implement IId and have a DocumentMap, you won't
        // be able to insert, update or delete them.
        class Result
        {
            public string FullName { get; set; }
            public string Email { get; set; }
        }
        
        [Test, Order(8)]
        public void QueryWithArbitraryType()
        {
            using var transaction = store.BeginTransaction();

            // This pattern can be used for just about any quick query. For this to work, property names on the type
            // must match the name of columns from the result set.
            var result = transaction.Stream<Result>(
                "select FirstName + ' ' + LastName as FullName, Email from dbo.Person order by FirstName"
            ).First();
            
            result.FullName.Should().Be("Buggs Bunny");
        }
        
        // Custom types can be used when reading data rows into a type that Nevermore doesn't directly handle. For 
        // example, here's a custom type handler for URIs. Do it by implementing ITypeHandler. It's a good idea to 
        // also make it a JSON converter, so that you can store the type either in columns or in the JSON blob at the
        // end.
        public class UriTypeHandler : JsonConverter, ITypeHandler
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Uri);
            }

            public object ReadDatabase(DbDataReader reader, int columnIndex)
            {
                if (reader.IsDBNull(columnIndex))
                    return default(Uri);
                var text = reader.GetString(columnIndex);
                return new Uri(text);
            }

            public void WriteDatabase(DbParameter parameter, object value)
            {
                parameter.Value = ((Uri) value)?.ToString();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var text = reader.ReadAsString();
                if (text == null)
                    return default(Uri);
                return new Uri(text);
            }
            
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteValue(((Uri)value).ToString());
            }
        }
        
        [Test, Order(9)]
        public void CustomTypes()
        {
            using var transaction = store.BeginTransaction();
            
            // Call this before you first read records of this type
            store.Configuration.TypeHandlerRegistry.Register(new UriTypeHandler());

            var result = transaction.Stream<(Uri HomePage, Uri SignIn)>(
                "select 'https://octopus.com' as Homepage, 'https://octopus.com/signin' as SignIn"
            ).First();
            
            result.HomePage.Should().Be(new Uri("https://octopus.com"));
            result.SignIn.Should().Be(new Uri("https://octopus.com/signin"));
        }
    }
}