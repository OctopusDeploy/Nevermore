using System;
using System.Xml.Linq;
using Nevermore.Mapping;
using Xunit;

namespace Nevermore.Tests.Mapping
{
    public class AmazingConverterFixture
    {
        [Fact]
        public void CanConvert()
        {
            Assert.Equal(null, AmazingConverter.Convert(null, typeof (string)));
            Assert.Equal(0, AmazingConverter.Convert(null, typeof (int)));
            Assert.Equal(0.0f, AmazingConverter.Convert(0, typeof (float)));
            Assert.Equal(0, AmazingConverter.Convert(0L, typeof (int)));
            Assert.Equal("0", AmazingConverter.Convert(0, typeof (string)));
            Assert.Equal(DateTime.MinValue, AmazingConverter.Convert(null, typeof (DateTime)));
            Assert.Equal(35, AmazingConverter.Convert("35", typeof (int)));
            Assert.Equal((XName)"button", AmazingConverter.Convert("button", typeof (XName)));
        }
    }
}