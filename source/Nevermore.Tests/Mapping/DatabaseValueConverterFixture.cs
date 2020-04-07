using System;
using System.Xml.Linq;
using FluentAssertions;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.Tests.Mapping
{
    public class DatabaseValueConverterFixture
    {
        [Test]
        public void CanConvert()
        {
            var databaseValueConverter = new DatabaseValueConverter(new RelationalStoreConfiguration());
            
            databaseValueConverter.ConvertFromDatabaseValue(null, typeof(string)).Should().BeNull();
            databaseValueConverter.ConvertFromDatabaseValue(null, typeof (int)).Should().Be(0);
            databaseValueConverter.ConvertFromDatabaseValue(0, typeof (float)).Should().Be(0.0f);
            databaseValueConverter.ConvertFromDatabaseValue(0L, typeof (int)).Should().Be(0);
            databaseValueConverter.ConvertFromDatabaseValue(0, typeof (string)).Should().Be("0");
            databaseValueConverter.ConvertFromDatabaseValue(null, typeof (DateTime)).Should().Be(DateTime.MinValue);
            databaseValueConverter.ConvertFromDatabaseValue("35", typeof (int)).Should().Be(35);
            databaseValueConverter.ConvertFromDatabaseValue("button", typeof (XName)).Should().Be((XName)"button");
        }
    }
}