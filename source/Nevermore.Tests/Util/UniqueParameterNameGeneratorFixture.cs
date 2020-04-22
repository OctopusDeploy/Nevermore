using FluentAssertions;
using NUnit.Framework;

namespace Nevermore.Tests.Util
{
    public class UniqueParameterNameGeneratorFixture
    {
        [Test]
        public void Generator()
        {
            var generator = new UniqueParameterNameGenerator();
            generator.GenerateUniqueParameterName("foo").Should().Be("foo");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_1");
            generator.GenerateUniqueParameterName("FOO").Should().Be("foo_2");
            generator.GenerateUniqueParameterName("baz").Should().Be("baz");
            generator.GenerateUniqueParameterName("baz").Should().Be("baz_1");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_3");
            
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_4");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_5");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_6");
            generator.Return(new[] { "foo_4", "foo_5" });
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_4");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_5");
            
            generator.Return(new[] { "foo", "foo_1" });
            generator.GenerateUniqueParameterName("foo").Should().Be("foo");
            generator.GenerateUniqueParameterName("foo").Should().Be("foo_1");
        }
    }
}