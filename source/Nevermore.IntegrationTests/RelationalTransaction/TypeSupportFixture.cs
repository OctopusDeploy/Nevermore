using System;
using System.Linq;
using System.Text;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;
// ReSharper disable RedundantTypeArgumentsOfMethod

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    [TestFixture]
    public class TypeSupportFixture : FixtureWithRelationalStore
    {
        [Test]
        public void SupportsTypes()
        {
            AssertCanRead<string>("hello", "convert(nvarchar(50), 'hello')");
            AssertCanRead<string>("hello", "convert(varchar(50), 'hello')");
            AssertCanRead<string>("hello", "convert(varchar(max), 'hello')");
            AssertCanRead<string>("hello", "convert(nvarchar(max), 'hello')");
            AssertCanRead<string>(null, "convert(nvarchar(50), null)");
            
            AssertCanRead<short>(17, "convert(smallint, 17)");
            AssertCanRead<short?>(17, "convert(smallint, 17)");
            AssertCanRead<short?>(null, "convert(smallint, null)");
            
            AssertCanRead<int>(17, "convert(int, 17)");
            AssertCanRead<int?>(17, "convert(int, 17)");
            AssertCanRead<int?>(null, "convert(int, null)");
            
            AssertCanRead<long>(17, "convert(bigint, 17)");
            AssertCanRead<long?>(17, "convert(bigint, 17)");
            AssertCanRead<long?>(null, "convert(bigint, null)");
            
            AssertCanRead<decimal>(100.10m, "convert(decimal(5,2), 100.10)");
            AssertCanRead<decimal?>(100.10m, "convert(decimal(5,2), 100.10)");
            AssertCanRead<decimal?>(null, "convert(decimal(5,2), null)");
            
            AssertCanRead<DateTime>(new DateTime(2010, 11, 14), "convert(datetime, '2010-11-14')");
            AssertCanRead<DateTime?>(new DateTime(2010, 11, 14), "convert(datetime, '2010-11-14')");
            AssertCanRead<DateTime?>(null, "convert(datetime, null)");
            
            AssertCanRead<DateTimeOffset>(new DateTimeOffset(2010, 11, 14, 0, 0, 0, TimeSpan.Zero), "convert(datetimeoffset, '2010-11-14')");
            AssertCanRead<DateTimeOffset?>(new DateTimeOffset(2010, 11, 14, 0, 0, 0, TimeSpan.Zero), "convert(datetimeoffset, '2010-11-14')");
            AssertCanRead<DateTimeOffset?>(null, "convert(datetime, null)");
            
            AssertCanRead<byte[]>(Encoding.UTF8.GetBytes("hello"), "convert(varbinary(30), 'hello')");
            AssertCanRead<byte[]>(null, "convert(varbinary, null)");
            
            AssertCanRead<MyEnum>(MyEnum.Foo, "convert(nvarchar(50), 'Foo')");
            AssertCanRead<MyEnum>(MyEnum.Bar, "convert(nvarchar(50), 'Bar')");
            AssertCanRead<MyEnum>(MyEnum.Bar, "convert(nvarchar(50), 'BAR')");
            AssertCanRead<MyEnum>(MyEnum.Foo, "convert(int, 1)");
            AssertCanRead<MyEnum>(MyEnum.Bar, "convert(int, 2)");
            AssertCanRead<MyEnum?>(MyEnum.Foo, "convert(nvarchar(50), 'Foo')");
            AssertCanRead<MyEnum?>(MyEnum.Bar, "convert(nvarchar(50), 'Bar')");
            AssertCanRead<MyEnum?>(MyEnum.Bar, "convert(nvarchar(50), 'BAR')");
            AssertCanRead<MyEnum?>(null, "convert(nvarchar(50), null)");
            AssertCanRead<MyEnum?>(MyEnum.Foo, "convert(int, 1)");
            AssertCanRead<MyEnum?>(MyEnum.Bar, "convert(int, 2)");
            AssertCanRead<MyEnum?>(null, "convert(int, null)");
            AssertCanRead<MyEnum?>(null, "convert(int, null)");

            AssertCanRead<MyEnum>(MyEnum.All, "convert(nvarchar(50), 'All')");
            AssertCanRead<MyEnum>(MyEnum.All, "convert(int, 3)");
        }

        [Flags]
        enum MyEnum
        {
            Foo = 1,
            Bar = 2,
            All = Foo | Bar,
        }

        void AssertCanRead<T>(T expected, string selectColumn)
        {
            using var transaction = Store.BeginReadTransaction();

            var resultFromPrimitive = transaction.Stream<T>($"select ({selectColumn}) as Column1").First();
            Assert.AreEqual(resultFromPrimitive, expected);
            
            var resultFromTuple = transaction.Stream<(T Val1, int Val2)>($"select ({selectColumn}) as Val1, 7 as Val2").First();
            Assert.AreEqual(resultFromTuple.Val1, expected);
            Assert.AreEqual(resultFromTuple.Val2, 7);
        }
    }
}