namespace Nevermore
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IRelationalTransaction transaction, TDocument instance) where TDocument : class, IId;
    }
}
