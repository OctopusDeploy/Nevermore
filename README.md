# Nevermore

Nevermore allows you to work with SQL Server as if it were a JSON document store. Nevermore has been used in production inside of Octopus Deploy since version 3.0 (work started in 2014), when we switched from RavenDB to SQL Server. We enjoyed using RavenDB and wanted to keep many of the document concepts, but found it was easier for customers to run Octopus on top of SQL Server, so we built Nevermore and switched Octopus from [RavenDB to SQL Server](https://octopus.com/blog/3.0-switching-to-sql).

Nevermore has some simple principles:

 - C# classes (Order, Customer, etc.) can be defined as documents
 - Documents can have have nested properties, lists, etc. - anything that can be serialized as JSON
 - Each document gets its own database table (Orders, Customers, etc.)
 - The document ID is the primary key of the table. Like Raven, it uses Hi-Lo to generate string identifiers ("Order-123")
 - Some document properties can be stored as regular columns on the table, for faster querying
 - All other properties are stored in a JSON blob

If you run on SQL Server 2016 and above, you can query the JSON blob using SQL Server's `JSON_VALUE` functions. 

# Example usage

## Define documents

The documents used by Nevermore are simple POCO classes - if you can serialize it into JSON, you can store it as a document. For this example and `Order` documents that has multiple `OrderLine`s. The `IId` interface marks the class as a root document.

```csharp
class Order : IId
{
    public string Id { get; set; }
    public bool Completed { get; set; }
    public string Buyer { get; set; }
    public List<OrderLine> OrderLines { get; set; }
}

class OrderLine
{
    public string Description { get; set; }
    public decimal Amount { get; set; }
}
```


## Table structure

Each document you want to store is defined as a class in C#, and each type of document is mapped to a different SQL table. 

```sql
CREATE TABLE [Order]  
(
    Id nvarchar(100) NOT NULL PRIMARY KEY, 
    Completed bit not null, 
    JSON nvarchar(max) not null
) 
```

While most of the document is serialized as JSON and stored in the last column, you often want to query for documents by different fields. You can do this by having Nevermore store them as separate columns on the table. 

Therefore, each table has:

 - An `Id` column which is always a string
 - Zero or more custom columns
 - A `JSON` column for storing all other document properties

## Create mappings

Next the mappings between the .NET classes and database need to be defined. Nevermore knows how to treat the ID property, and knows where to store all other properties as JSON, but you need to tell it how to store the custom columns. 

```csharp
class OrderMap : DocumentMap<Order>
{
    public OrderMap()
    {
        Column(o => o.Completed);
    }
}
```

## Set up the store
One `RelationalStore` instance per application should be created. This store can be used on multiple threads to interact with the database.

```csharp
// Define how the documents are serialized
var jsonSettings = new JsonSerializerSettings
{
    DateFormatHandling = DateFormatHandling.IsoDateFormat,
    DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
    TypeNameHandling = TypeNameHandling.Auto,
    TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
};

// Add the document mappings
var mappings = new RelationalMappings();
mappings.Install(new[] { new OrderMap() });

var store = new RelationalStore(
    connectionString,
    "DemoApp",
    new SqlCommandFactory(), 
    mappings,
    jsonSettings,
    new EmptyRelatedDocumentStore()
);
```

## Setting up the database

Nevermore has some embedded scripts that setup the database with the required tables
and stored procedures. The easiest way manage and run the database migrations
is to use [DbUp](https://github.com/DbUp/DbUp). 

To run the migrations:

```csharp
string connectionString = "Server=localhost;Database=NevermoreDemo;Trusted_Connection=True";

// Setup the database
DbUp.DeployChanges.To
    .SqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssemblies(new[] { 
        typeof(RelationalStore).Assembly, // Contains the Nevermore required script
        this.GetType().Assembly  // Your scripts
    })
    .Build()
    .PerformUpgrade();
```

Remember that any upgrade scripts must be included as an **embedded resource**.

## Set up transient fault handling
Nevermore is configured with various strategies to handling transient faults; the default is to exponentially back off when retrying. No matter which route you take regarding transient faults, you still need to initialize the transient fault handler at application start - put this line in your `Startup.cs` (or equivalent location):

```csharp
TransientFaultHandling.InitializeRetryManager();
```

Without this line, you will receive an error that "Retry manager not set" when trying to begin a new transaction.

## Add an item to the database

Nevermore is designed for predictable performance, and doesn't try to abstract SQL Server away from you too much. It doesn't provide a fancy Unit-of-Work, caching, change tracking object like Entity Framework's DbContext. Instead, it exposes a basic Nevermore-aware wrapper around the SQL transaction that you interact with. You'll need to be specific about inserting or updating objects and committing the transaction. 

```csharp
using (var trn = store.BeginTransaction())
{
    // Nevermore can store documents of arbitrary depth
    var order = new Order()
    {
        Buyer = "Someone",
        OrderLines = new List<UserQuery.OrderLine>
        {
            new OrderLine { Description = "Widget", Amount = 4.55m},
            new OrderLine { Description = "Thingi", Amount = 0.55m}
        }
    };

    trn.Insert(order);
    trn.Commit();
}
```

## Retrieve it by query and updating it

When querying data, it turns out SQL Server has a pretty good DSL for querying databases... called SQL. So you can query for documents using SQL, and making use of query parameters. 

```csharp
string orderId; // The id for the next example

using (var trn = store.BeginTransaction())
{
    var order = trn.Query<Order>()
        .Where("Completed = @completed")   // Any valid SQL where clause
        .Parameter("completed", false)     // Parameters to pass to the query (don't concatenate strings)
        .First();

    orderId = order.Id;
    order.Completed = true;

    trn.Update(order);
    trn.Commit();
}
```

Make sure you call Update on the object to update - there's no Unit of Work manager doing change tracking trying to outsmart you. 

If you really dislike embedding SQL queries (you do realize it's translated to SQL anyway, right?) you can use some builders:

```
var order = trn.Query<Order>()
     .Where(nameof(Order.Completed), SqlOperand.Equal, false)
     .First();
```

## Retrieve by Id

Fetching objects by ID is very common, so you can do that directly:

```csharp
using (var trn = store.BeginTransaction())
{
    var order = trn.Load<Order>(orderId);
    Console.WriteLine($"{order.Id} {order.Buyer} {order.Completed}");
    foreach(var line in order.OrderLines)
        Console.WriteLine($"{line.Description} {line.Amount:n2}");
}
```

## Other features

Nevermore is inspired by micro-ORM's like Dapper, and like most micro-ORM's doesn't try to hide the fact that you are using SQL Server. But it adds JSON storage as a prime concept. This gives lots of advantages. 

 - You get all the regular benefits of SQL Server - indexing, partitioning, a well-known universe of tooling and operations support
 - You can query against views (just `Map` a document class to a view instead of a table)
 - You can run and query stored procedures
 - You can still use foreign keys, but you don't need to
 
