using FluentAssertions;
using Nevermore.TypedStrings;
using NUnit.Framework;

namespace Nevermore.Tests.TypedStrings
{
    [TestFixture]
    public class TypedStringFixture
    {
        [Test]
        [TestCase(null, null)]
        [TestCase("some-value", "some-value")]
        public void ImplicitConversionToString_OfWrappedValue_ReturnsInnerValue(string innerValue, string expectedImplicitlyConvertedValue)
        {
            var typedString = new SomeTypedString(innerValue);

            ImplicitlyConvertedToString(typedString).Should().Be(expectedImplicitlyConvertedValue);
        }

        [Test]
        public void ImplicitConversionToString_OfNull_ReturnsNull()
        {
            SomeTypedString typedString = null;

            // ReSharper disable once ExpressionIsAlwaysNull
            ImplicitlyConvertedToString(typedString).Should().BeNull();
        }

        string ImplicitlyConvertedToString(string typedStringImplicitlyConverted)
        {
            return typedStringImplicitlyConverted;
        }

        class SomeTypedString : TypedString
        {
            public SomeTypedString(string value) : base(value)
            {
            }
        }
    }
}