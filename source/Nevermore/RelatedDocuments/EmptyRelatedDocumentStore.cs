using System.Collections.Generic;

namespace Nevermore.RelatedDocuments
{
    public class EmptyRelatedDocumentStore : IRelatedDocumentStore
    {
        public void PopulateRelatedDocuments<TDocument>(IWriteTransaction transaction, TDocument instance) where TDocument : class
        {
        }

        public void PopulateManyRelatedDocuments<TDocument>(IWriteTransaction transaction, IEnumerable<TDocument> instance) where TDocument : class
        {
        }
    }
}