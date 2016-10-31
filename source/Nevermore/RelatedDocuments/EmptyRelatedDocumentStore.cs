namespace Nevermore
{
    public class EmptyRelatedDocumentStore : IRelatedDocumentStore
    {
        public void PopulateRelatedDocuments<TDocument>(IRelationalTransaction transaction, TDocument instance) where TDocument : class, IId
        {
        }
    }
}