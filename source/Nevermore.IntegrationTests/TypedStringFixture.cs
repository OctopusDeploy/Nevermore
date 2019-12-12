using Nevermore.IntegrationTests.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    [TestFixture]
    public class TypedStringFixture
    {
        [Test]
        public void TODO_TODO_TODO()
        {
            IQueryExecutor queryExecutor = null;

            var productId = new ProductId("le product");
            queryExecutor.LoadRequired2<Product, ProductId>(productId);

            Product product = null;
            queryExecutor.Insert<Product, ProductId>(product);
        }

    }
}