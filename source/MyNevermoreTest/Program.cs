// See https://aka.ms/new-console-template for more information

using MyNevermoreTest;
using Nevermore;

Console.WriteLine("Hello, World!");

var connStr = "Server=localhost;Database=MyNevermoreTest;Trusted_Connection=True;TrustServerCertificate=True";

// You just need a SQL Server connection string
var config = new RelationalStoreConfiguration(connStr);

// And tell Nevermore about your document maps. 
config.DocumentMaps.Register(new StudentMap());

// Create your store. You'll do this once when the application starts up.
var store = new RelationalStore(config);

// var student = new Student
// {
//     Age = 0,
//     Email = "email1"
// };

// using var tx = store.BeginTransaction();
// tx.Insert(student);
// tx.Commit();

using var trn = store.BeginTransaction();
var student = trn.Query<Student>().Stream().Single();
student.Age += 3;
trn.Update(student);

// sleep and manually kill the connection

student.Age -= 1;
trn.Update(student);

trn.Replay();

trn.Commit();
