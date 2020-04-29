using System.Collections.Generic;
using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient.Server;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class TableValuesParametersFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            NoMonkeyBusiness();
            
            ExecuteSql("create table dbo.SomeTable ([Id] nvarchar(50), [Name] nvarchar(50), [References] nvarchar(50))");
            ExecuteSql("create type dbo.SomeTableInsertData as table ([Id] nvarchar(50), [Name] nvarchar(50), [References] nvarchar(50))");
        }

        [Test]
        public void ShouldBulkInsert()
        {
            using var writer = Store.BeginWriteTransaction();
            
            var idMetaData = new SqlMetaData("Id", SqlDbType.NVarChar, 50);
            var nameMetadata = new SqlMetaData("Name", SqlDbType.NVarChar, 50);
            var referencesMetadata = new SqlMetaData("References", SqlDbType.NVarChar, 50);
            
            var records = new List<SqlDataRecord>();
            for (var i = 0; i < 100000; i++)
            {
                var record = new SqlDataRecord(idMetaData, nameMetadata, referencesMetadata);
                record.SetString(0, "MyId-" + i);
                record.SetString(1, "Name for " + i);
                record.SetString(2, "Some-Other-Doc-" + i);
                records.Add(record);
            }

            var parameters = new CommandParameterValues();
            parameters.AddTable("bulkInsertData", new TableValuedParameter("dbo.SomeTableInsertData", records));
            
            writer.ExecuteNonQuery("insert into dbo.SomeTable ([Id], [Name], [References]) select [Id], [Name], [References] from @bulkInsertData", parameters);

            var count = writer.ExecuteScalar<int>("select count(*) from dbo.SomeTable");
            count.Should().Be(100000);
            
            writer.Commit();
        }
    }
}