using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced.Queryable;
using NUnit.Framework;

namespace Nevermore.Tests.Queryable
{
    public class QueryableExtensionsFixture
    {
        [Test]
        public void HintCanBeTranslatedByEnumerableQuery()
        {
            var list = new List<string>()
            {
                "hello",
                "there"
            }.AsQueryable();

            var thing = list.Hint("ABC").FirstOrDefault(s => s == "hello");

            thing.Should().Be("hello");
        }
    }
}