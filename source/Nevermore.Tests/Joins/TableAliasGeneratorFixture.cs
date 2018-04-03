using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Nevermore.Tests.Joins
{
    public class TableAliasGeneratorFixture
    {
        [Fact]
        public void ShouldGenerateUniqueAliases()
        {
            var results = new HashSet<string>();

            var target = new TableAliasGenerator();

            for (int i = 0; i < 100; i ++)
            {
                var actual = target.GenerateTableAlias();

                Assert.False(results.Contains(actual));
                results.Add(actual);
            }
        }

        [Fact]
        public void ShouldGenerateDeterministicAliases()
        {
            var firstGenerator = new TableAliasGenerator();
            var secondGenerator = new TableAliasGenerator();
            foreach (var _ in Enumerable.Range(0, 100))
                firstGenerator.GenerateTableAlias().Should().BeEquivalentTo(secondGenerator.GenerateTableAlias());
        }
    }
}