using System;
using System.Xml.Linq;
using FluentAssertions;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.Tests.Mapping
{
    public class AmazingConverterFixture
    {
        [Test]
        public void CanConvert()
        {
            var amazingConverter = new AmazingConverter(new RelationalStoreConfiguration(null));
            
            amazingConverter.Convert(null, typeof(string)).Should().BeNull();
            amazingConverter.Convert(null, typeof (int)).Should().Be(0);
            amazingConverter.Convert(0, typeof (float)).Should().Be(0.0f);
            amazingConverter.Convert(0L, typeof (int)).Should().Be(0);
            amazingConverter.Convert(0, typeof (string)).Should().Be("0");
            amazingConverter.Convert(null, typeof (DateTime)).Should().Be(DateTime.MinValue);
            amazingConverter.Convert("35", typeof (int)).Should().Be(35);
            amazingConverter.Convert("button", typeof (XName)).Should().Be((XName)"button");
        }
    }
}