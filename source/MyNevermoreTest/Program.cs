﻿// See https://aka.ms/new-console-template for more information

using MyNevermoreTest;
using Nevermore;

var connStr = args[0];

// You just need a SQL Server connection string
var config = new RelationalStoreConfiguration(connStr);

// And tell Nevermore about your document maps. 
config.DocumentMaps.Register(new StudentMap());

// Create your store. You'll do this once when the application starts up.
var store = new RelationalStore(config);

//CreateStudent();

using var trn = store.BeginTransaction();
var student = trn.Query<Student>().Stream().Single();
student.Age += 3;
trn.Update(student);

// sleep and manually kill the connection

student.Age -= 1;
trn.Update(student);

trn.Replay();

trn.Commit();

void CreateStudent()
{
    using var tx = store.BeginTransaction();
    var student = new Student
    {
        Age = 0,
        Email = "email1"
    };

    tx.Insert(student);
    tx.Commit();
}
