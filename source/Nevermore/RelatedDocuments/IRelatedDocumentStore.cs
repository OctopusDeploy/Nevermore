using System.Collections.Generic;

namespace Nevermore.RelatedDocuments
{
    public interface IRelatedDocumentStore
    {
        void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class;
        void PopulateManyRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class;
    }
}
