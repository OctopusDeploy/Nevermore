using System.Collections.Generic;

namespace Nevermore.RelatedDocuments
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class;
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class;
    }
}
