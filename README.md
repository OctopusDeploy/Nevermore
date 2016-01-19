# Nevermore
A JSON Document Store library for SQL Server

## Database upgrade scripts with DbUp
Nevermore runs its own initial schema script which will show up in `SchemaVersions` as `Nevermore.UpgradeScripts.Script0001 - Initial schema.sql`. Additional DbUp transforms can be performed by including something like the following:
```
DeployChanges.To
    .SqlDatabase(store.ConnectionString)
    .WithScriptsAndCodeEmbeddedInAssembly(typeof(IRelationalStoreFactory).Assembly)
    .LogScriptOutput()
    .WithVariable("databaseName", new SqlConnectionStringBuilder(store.ConnectionString).InitialCatalog)
    .LogTo(log)
    .Build();
```

Remember that any upgarde scripts must be included as an **embedded resource**.
