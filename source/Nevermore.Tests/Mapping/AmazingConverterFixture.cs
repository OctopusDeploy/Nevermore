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
            AmazingConverter.Convert(null, typeof(string)).Should().BeNull();
            AmazingConverter.Convert(null, typeof (int)).Should().Be(0);
            AmazingConverter.Convert(0, typeof (float)).Should().Be(0.0f);
            AmazingConverter.Convert(0L, typeof (int)).Should().Be(0);
            AmazingConverter.Convert(0, typeof (string)).Should().Be("0");
            AmazingConverter.Convert(null, typeof (DateTime)).Should().Be(DateTime.MinValue);
            AmazingConverter.Convert("35", typeof (int)).Should().Be(35);
            AmazingConverter.Convert("button", typeof (XName)).Should().Be((XName)"button");
        }
    }
}