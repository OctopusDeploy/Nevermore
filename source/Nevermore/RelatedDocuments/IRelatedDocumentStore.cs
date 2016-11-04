using Nevermore.Contracts;

namespace Nevermore.RelatedDocuments
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IRelationalTransaction transaction, TDocument instance) where TDocument : class, IId;
    }
}
