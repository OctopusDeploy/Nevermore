-- Based on the StackExchange Dapper benchmark
Create Table Posts
(
    Id nvarchar(200) primary key not null, 
    [Text] varchar(max) not null, 
    CreationDate datetime not null, 
    LastChangeDate datetime not null,
    Counter1 int,
    Counter2 int,
    Counter3 int,
    Counter4 int,
    Counter5 int,
    Counter6 int,
    Counter7 int,
    Counter8 int,
    Counter9 int
)

go

declare @i int = 0
While @i <= 5001
Begin
    Insert Posts (Id, [Text],CreationDate, LastChangeDate) values ('Posts-'+ convert(nvarchar(5), @i), replicate('x', 200), GETDATE(), GETDATE());
    Set @i = @i + 1;
End

go 

Create Table Customer
(
    Id nvarchar(200) primary key not null, 
    FirstName nvarchar(20) not null,    
    LastName nvarchar(20) not null,    
    Nickname nvarchar(200) null,    
    CreationDate datetime not null, 
    LastChangeDate datetime not null,
    Counter1 int,
    Counter2 int,
    Counter3 int,
    Counter4 int,
    Counter5 int,
    Counter6 int,
    Counter7 int,
    Counter8 int,
    Counter9 int,
    [Rowversion] rowversion not null,
    [JSON] nvarchar(max) not null
)

alter table Customer add constraint UQ_UniqueCustomerNames unique(FirstName, LastName)

go

begin transaction 
declare @i int = 0
While @i <= 50001
Begin
    Insert Customer (Id, FirstName, LastName, Nickname, CreationDate, LastChangeDate, Counter3, [JSON]) values ('Customer-'+ convert(nvarchar(10), @i), 'Robert', 'Menzies ' + convert(nvarchar(10), @i), 'Bob', (getdate()), (getdate()), @i, '{}')
    Set @i = @i + 1;
End

commit 

go 

Create Table BigObject
(
    Id nvarchar(200) primary key not null, 
    [JSON] nvarchar(max) not null
)

Create Table BigObjectCompressed
(
    Id nvarchar(200) primary key not null, 
    [JSONBlob] varbinary(max) not null
)

Create Table BigObjectMixed
(
    Id nvarchar(200) primary key not null, 
    [JSON] nvarchar(max) not null,
    [JSONBlob] varbinary(max) not null
)
