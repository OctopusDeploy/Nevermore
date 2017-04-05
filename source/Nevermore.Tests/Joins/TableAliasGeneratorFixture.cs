using System.Collections.Generic;
using Nevermore.Joins;
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
    }
}