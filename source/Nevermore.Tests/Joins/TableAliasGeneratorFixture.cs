using System.Collections.Generic;
using Nevermore.Joins;
using NUnit.Framework;

namespace Nevermore.Tests.Joins
{
    [TestFixture]
    public class TableAliasGeneratorFixture
    {
        [Test]
        public void ShouldGenerateUniqueAliases()
        {
            var results = new HashSet<string>();

            var target = new TableAliasGenerator();

            for (int i = 0; i < 100; i ++)
            {
                var actual = target.GenerateTableAlias();

                Assert.IsFalse(results.Contains(actual));
                results.Add(actual);
            }
        }
    }
}