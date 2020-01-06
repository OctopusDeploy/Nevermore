using Nevermore.Contracts;
using Nevermore.IntegrationTests.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    [TestFixture]
    public class TypedStringFixture
    {
        IQueryExecutor executor;

        [Test]
        public void TODO_TODO_TODO()
        {
            executor = null;

            var productId = new ProductId("le product");
            executor.LoadRequired<Product, ProductId>(productId);
            LoadWrapped2(productId);

            Product product = null;
            executor.Insert(product); //Inserts are fine
        }

        TDocument LoadWrapped<TDocument>(IIdWrapper id) where TDocument : class, IId<IIdWrapper>
        {
            //Only places that you are providing just the ID needs the generic arguments
            var doc = executor.LoadRequired<TDocument, IIdWrapper>(id);

            return doc;
        }

        //This lets us get away from having to specify the generic arguments by coupling the ID to the document (maybe yuck?)
        TDocument LoadWrapped2<TDocument>(IIdWrapperCoupledToDocument<TDocument> id) where TDocument : class, IId<IIdWrapper>
        {
            var doc = executor.LoadRequired<TDocument, IIdWrapper>(id);

            return doc;
        }

    }
}